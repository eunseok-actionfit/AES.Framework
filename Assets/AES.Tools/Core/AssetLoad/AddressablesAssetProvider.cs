using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if AESFW_UNITASK


#elif !UNITY_2023_1_OR_NEWER
using System.Threading;
using System.Threading.Tasks;
#else
using System.Threading;
#endif

namespace AES.Tools
{
    public class AddressablesAssetProvider : IAssetProvider
    {
        private class HandleInfo
        {
            public AsyncOperationHandle Handle;
            public int RefCount;
        }

        private readonly Dictionary<string, HandleInfo> _handles = new();
        private readonly Dictionary<object, string> _assetToKey = new(); // ← 추가


        // IAssetProvider.LoadAsync<T> 시그니처와 반드시 동일해야 함
#if AESFW_UNITASK
        public async UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object
#elif UNITY_2023_1_OR_NEWER
    public async Awaitable<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object
#else
    public async Task<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object
#endif
        {
            // 이미 로드된 경우 ref count 증가 후, 취소 한 번 체크
            if (_handles.TryGetValue(key, out var info))
            {
                info.RefCount++;
                ct.ThrowIfCancellationRequested();
                return (T)info.Handle.Result;
            }

            var handle = Addressables.LoadAssetAsync<T>(key);
            var newInfo = new HandleInfo
            {
                Handle = handle,
                RefCount = 1
            };
            _handles[key] = newInfo;

            try
            {
                ct.ThrowIfCancellationRequested();

#if AESFW_UNITASK
                // UniTask로 변환해서 CancellationToken 전달
                await handle.ToUniTask(cancellationToken: ct);
#else
            // 여기서는 Addressables 자체가 토큰을 받지 않기 때문에
            // 로드 완료 후/전후에 취소 여부만 체크
            await handle.Task;
            ct.ThrowIfCancellationRequested();
#endif

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    _handles.Remove(key);
                    throw new System.Exception($"[AssetLoader] Failed to load asset: {key}");
                }

                _assetToKey[handle.Result] = key;
                return handle.Result;
            }
            catch
            {
                // 취소/에러 시 핸들 정리
                _handles.Remove(key);
                Addressables.Release(handle);
                throw;
            }
        }

        public void Release(string key)
        {
            if (!_handles.TryGetValue(key, out var info))
                return;

            info.RefCount--;
            if (info.RefCount <= 0)
            {
                Addressables.Release(info.Handle);
                _assetToKey.Remove(info.Handle.Result);
                _handles.Remove(key);
            }
        }
        
        public void Release(object asset)
        {
            if (asset == null || !_assetToKey.TryGetValue(asset, out var key))
                return;
    
            Release(key);
        }


        public void ReleaseAll()
        {
            foreach (var kvp in _handles)
            {
                Addressables.Release(kvp.Value.Handle);
            }
            _handles.Clear();
        }

        public bool IsLoaded(string key)
        {
            return _handles.ContainsKey(key);
        }

#if AESFW_UNITASK
        public async UniTask<IReadOnlyList<T>> LoadByLabelAsync<T>(
            string label,
            CancellationToken ct = default
        ) where T : Object
#elif UNITY_2023_1_OR_NEWER
        public async Awaitable<IReadOnlyList<T>> LoadByLabelAsync<T>(
            string label,
            CancellationToken ct = default
        ) where T : Object
#else
        public async Task<IReadOnlyList<T>> LoadByLabelAsync<T>(
            string label,
            CancellationToken ct = default
        ) where T : Object
#endif
        {
            // 라벨로 에셋들을 한 번에 로드 (Addressables 기본 기능)
            var handle = Addressables.LoadAssetsAsync<T>(
                label,
                callback: null,        // 개별 콜백이 필요하면 여기서 처리
                releaseDependenciesOnFailure: false
            );

            try
            {
#if AESFW_UNITASK
                await handle.ToUniTask(cancellationToken: ct);
#else
                await handle.Task;
                ct.ThrowIfCancellationRequested();
#endif
                if (handle.Status != AsyncOperationStatus.Succeeded)
                    throw new System.Exception($"[AssetLoader] Failed to load label: {label}");

                // 결과 각각에 대해 내부 ref-count 관리에 편입시키고 싶다면
                foreach (var asset in handle.Result)
                {
                    if (!_assetToKey.ContainsKey(asset))
                    {
                        // 라벨로 로드한 에셋의 키를 어떻게 둘지 정책 필요
                        // 여기선 단순히 asset을 key로 쓰는 예시
                        var key = asset.name; // 또는 다른 정책
                        _assetToKey[asset] = key;

                        _handles[key] = new HandleInfo
                        {
                            Handle = handle,  // 여러 에셋이 하나의 handle을 공유
                            RefCount = 1
                        };
                    }
                }

                return (IReadOnlyList<T>)handle.Result;
            }
            catch
            {
                Addressables.Release(handle);
                throw;
            }
        }
    }
}