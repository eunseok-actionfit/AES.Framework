#if AESFW_UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;
#endif
using UnityEngine;


public interface IAssetProvider
{
#if AESFW_UNITASK
    UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object;
#elif UNITY_2023_1_OR_NEWER
    Awaitable<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object;
#else
    Task<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Object;
#endif

    void Release(string key);
    void ReleaseAll();
    bool IsLoaded(string key);
}