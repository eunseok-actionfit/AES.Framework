// using System;
// using System.Threading;
// using UnityEngine;
//
// #if AESFW_UNITASK
// using Cysharp.Threading.Tasks;
// #elif UNITY_2023_1_OR_NEWER
// using UnityEngine; // Awaitable<T>
// #else
// using System.Threading.Tasks;
// #endif
//
// namespace Core.Engine.Factory
// {
//     public sealed class SimpleViewFactory<T> : IAsyncFactory<T> where T : Component
//     {
//         readonly IAssetInstantiator _inst;
//         readonly string _key;
//
//         public SimpleViewFactory(IAssetInstantiator inst)
//         {
//             _inst = inst;
//             var attr = (AssetKeyAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(AssetKeyAttribute));
//             _key = attr?.Key ?? throw new InvalidOperationException($"[{typeof(T).Name}]에 AssetKeyAttribute가 없음");
//         }
//
// #if AESFW_UNITASK
//         public async UniTask<T> CreateAsync(CancellationToken ct = default)
//         {
//             await UniTask.SwitchToMainThread();
//
//             var go = await _inst.InstantiateAsync(_key, null, false, ct);
//             var comp = go.GetComponent<T>();
//             if (!comp) throw new Exception($"AssetKey[{_key}]에 {typeof(T).Name} 미부착");
//
//             return comp;
//         }
// #elif UNITY_2023_1_OR_NEWER
//         public async Awaitable<T> CreateAsync(CancellationToken ct = default)
//         {
//             // 여기서는 IAssetInstantiator.InstantiateAsync가 어떤 타입을 리턴하는지에 따라 맞춰줘야 함
//             // 가정: IAssetInstantiator.InstantiateAsync도 IAsyncFactory와 같은 분기를 타고 있음
//
//             var go = await _inst.InstantiateAsync(_key, null, false, ct);
//             var comp = go.GetComponent<T>();
//             if (!comp) throw new Exception($"AssetKey[{_key}]에 {typeof(T).Name} 미부착");
//
//             return comp;
//         }
// #else
//         public async Task<T> CreateAsync(CancellationToken ct = default)
//         {
//             var go = await _inst.InstantiateAsync(_key, null, false, ct);
//             var comp = go.GetComponent<T>();
//             if (!comp) throw new Exception($"AssetKey[{_key}]에 {typeof(T).Name} 미부착");
//
//             return comp;
//         }
// #endif
//
//         public void Destroy(T item)
//         {
//             if (!item) return;
//             _inst.Release(item.gameObject);
//         }
//     }
// }
