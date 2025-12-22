using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressablesContentCache : IContentCache
{
    public async UniTask ClearAllAsync(CancellationToken ct)
    {
        await UniTask.SwitchToMainThread(ct);
        bool ok = Caching.ClearCache();
        if (!ok)
            throw new TransitionException(TransitionFailCode.ContentDownloadFailed, "Caching.ClearCache failed (cache may be in use).");
    }

    public async UniTask ClearByKeyAsync(object keyOrLabel, CancellationToken ct)
    {
        await UniTask.SwitchToMainThread(ct);

        AsyncOperationHandle<bool> h = default;
        try
        {
            h = Addressables.ClearDependencyCacheAsync(keyOrLabel, autoReleaseHandle: false);
            while (!h.IsDone)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield();
            }

            if (h.Status != AsyncOperationStatus.Succeeded || !h.Result)
            {
                throw new TransitionException(
                    TransitionFailCode.ContentDownloadFailed,
                    $"ClearDependencyCacheAsync failed for key: {keyOrLabel}",
                    h.OperationException);
            }
        }
        finally
        {
            if (h.IsValid()) Addressables.Release(h);
        }
    }

    public async UniTask CleanUnusedAsync(CancellationToken ct)
    {
        await UniTask.SwitchToMainThread(ct);

        // Addressables 버전에 따라 CleanBundleCache 시그니처가 다를 수 있음.
        // 컴파일이 안 되면 프로젝트 버전에 맞는 오버로드로 교체.
        AsyncOperationHandle h = default;
        try
        {
            h = Addressables.CleanBundleCache(null);
            while (!h.IsDone)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield();
            }

            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                throw new TransitionException(
                    TransitionFailCode.ContentDownloadFailed,
                    "CleanBundleCache failed",
                    h.OperationException);
            }
        }
        finally
        {
            if (h.IsValid()) Addressables.Release(h);
        }
    }
}
