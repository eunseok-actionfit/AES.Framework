using System;
using System.Threading;
using AES.Tools;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Engine.Factory
{
    public sealed class SimpleViewFactory<T> : IAsyncFactory<T> where T : Component
    {
        readonly IAssetProvider _asset;
        readonly string _key;

        public SimpleViewFactory(IAssetProvider asset)
        {
            _asset = asset ?? throw new ArgumentNullException(nameof(asset));

            var attr = (AssetKeyAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(AssetKeyAttribute));
            _key = attr?.Key ?? throw new InvalidOperationException($"[{typeof(T).Name}]에 AssetKeyAttribute가 없음");
        }

        public async UniTask<T> CreateAsync(CancellationToken ct = default)
        {
            await UniTask.SwitchToMainThread();

            // 인스턴스 생성
            var go = await _asset.InstantiateAsync(_key, ct);

            // 컴포넌트 체크
            var comp = go.GetComponent<T>();
            if (!comp)
                throw new InvalidOperationException($"AssetKey[{_key}]에 {typeof(T).Name} 미부착");

            return comp;
        }

        public void Destroy(T item)
        {
            if (!item)
                return;
            
            _asset.Release(item.gameObject);
        }
    }
}