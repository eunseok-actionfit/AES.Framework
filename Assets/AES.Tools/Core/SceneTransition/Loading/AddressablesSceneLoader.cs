using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer.Unity;


public sealed class AddressablesSceneLoader : ISceneLoader
{
    public UniTask UnloadScenesAsync(System.Collections.Generic.IReadOnlyList<Scene> scenes, CancellationToken ct)
    {
        // Addressables 씬은 Handle 기반 언로드가 안전.
        // "기존 씬 정리"는 Hybrid에서 UnitySceneLoader가 담당하도록 설계.
        return UniTask.CompletedTask;
    }

    public async UniTask<SceneHandle> LoadSceneAsync(
        ResolvedSceneKey key,
        bool additive,
        bool activateOnLoad,
        IProgress<float> progress,
        LifetimeScope parentScope,
        CancellationToken ct)
    {
        using (LifetimeScope.EnqueueParent(parentScope))
        {
            if (!key.IsAddressable || string.IsNullOrEmpty(key.ForAddressables))
                throw new TransitionException(TransitionFailCode.ContentNotFound, "Scene is not addressable (missing address).");

            var mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var handle = Addressables.LoadSceneAsync(key.ForAddressables, mode, activateOnLoad);

            while (!handle.IsDone)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(handle.PercentComplete);
                await UniTask.Yield();
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                var code = GuessAddrCode(handle.OperationException);
                throw new TransitionException(code, $"Addressables load failed: {key.ForAddressables}", handle.OperationException);
            }

            progress?.Report(1f);
            return new SceneHandle(key.ForAddressables, handle.Result.Scene, handle);
        }
    }

    public async UniTask ActivateAsync(SceneHandle handle, CancellationToken ct)
    {
        if (handle.NativeHandle is not AsyncOperationHandle<SceneInstance> ao) return;
        if (!ao.IsValid()) return;

        var act = ao.Result.ActivateAsync();
        while (!act.isDone)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }
    }

    public async UniTask UnloadAsync(SceneHandle handle, CancellationToken ct)
    {
        if (handle.NativeHandle is not AsyncOperationHandle<SceneInstance> ao) return;
        if (!ao.IsValid()) return;

        var unload = Addressables.UnloadSceneAsync(ao);
        while (!unload.IsDone)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }
    }

    private static TransitionFailCode GuessAddrCode(Exception e)
    {
        var m = (e?.Message ?? "").ToLowerInvariant();
        if (m.Contains("address") && (m.Contains("not found") || m.Contains("invalid") || m.Contains("no location") || m.Contains("unknown")))
            return TransitionFailCode.ContentNotFound;
        if (m.Contains("download") || m.Contains("cdn") || m.Contains("timeout") || m.Contains("network") || m.Contains("connection"))
            return TransitionFailCode.ContentDownloadFailed;
        return TransitionFailCode.SceneLoadFailed;
    }
}
