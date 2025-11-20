using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if AESFW_UNITASK
using Awaitable = Cysharp.Threading.Tasks.UniTask;
#elif UNITY_2023_1_OR_NEWER
// Unity 2023 Awaitable 사용 시: using Awaitable = UnityEngine.Awaitable<T>
#else
using Awaitable = System.Threading.Tasks.Task;
using System.Threading.Tasks;
#endif

namespace AES.Tools
{
    public static class AssetLoader
    {
        private static IAssetProvider _provider;

        public static void Initialize(IAssetProvider provider)
        {
            _provider = provider;
        }

        private static void EnsureInitialized()
        {
            if (_provider == null)
                throw new System.Exception("[AssetLoader] Not initialized. Call AssetLoader.Initialize() first.");
        }

        // --------------------
        // LoadAsync<T>
        // --------------------
#if AESFW_UNITASK
        public static UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object
#elif UNITY_2023_1_OR_NEWER
    public static Awaitable<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object
#else
    public static Task<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object
#endif
        {
            EnsureInitialized();
            return _provider.LoadAsync<T>(key, ct);
        }

        public static void Release(string key)
        {
            EnsureInitialized();
            _provider.Release(key);
        }

        public static void ReleaseAll()
        {
            EnsureInitialized();
            _provider.ReleaseAll();
        }

        public static bool IsLoaded(string key)
        {
            EnsureInitialized();
            return _provider.IsLoaded(key);
        }

        // --------------------
        // PreloadAsync<T>
        // --------------------
        public async static Awaitable PreloadAsync<T>(
            IEnumerable<string> keys,
            CancellationToken ct = default
        ) where T : Object
        {
            EnsureInitialized();

#if AESFW_UNITASK
            var tasks = new List<UniTask<T>>();
#elif UNITY_2023_1_OR_NEWER
        var tasks = new List<Awaitable<T>>();
#else
        var tasks = new List<Task<T>>();
#endif

            foreach (var key in keys)
            {
                ct.ThrowIfCancellationRequested();
                tasks.Add(_provider.LoadAsync<T>(key, ct));
            }

#if AESFW_UNITASK
            await UniTask.WhenAll(tasks);
#elif UNITY_2023_1_OR_NEWER
        // Unity Awaitable<T>에는 WhenAll이 없기 때문에 직접 await 필요
        foreach (var t in tasks)
            await t;
#else
        await Task.WhenAll(tasks);
#endif
        }
    }
}
