using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer.Unity;


public sealed class HybridSceneLoader : ISceneLoader
{
    private readonly UnitySceneLoader _unity;
    private readonly AddressablesSceneLoader _addr;

    public HybridSceneLoader(UnitySceneLoader unity, AddressablesSceneLoader addr)
    {
        _unity = unity;
        _addr = addr;
    }

    public UniTask UnloadScenesAsync(IReadOnlyList<Scene> scenes, CancellationToken ct)
        => _unity.UnloadScenesAsync(scenes, ct);

    public UniTask<SceneHandle> LoadSceneAsync(
        ResolvedSceneKey key,
        bool additive,
        bool activateOnLoad,
        IProgress<float> progress,
        LifetimeScope parentScope,
        CancellationToken ct)
    {
        return key.IsAddressable
            ? _addr.LoadSceneAsync(key, additive, activateOnLoad, progress, parentScope, ct)
            : _unity.LoadSceneAsync(key, additive, activateOnLoad, progress, parentScope, ct);
    }


    public UniTask ActivateAsync(SceneHandle handle, CancellationToken ct)
    {
        return handle.NativeHandle switch
        {
            AsyncOperationHandle<SceneInstance> => _addr.ActivateAsync(handle, ct),
            AsyncOperation => _unity.ActivateAsync(handle, ct),
            _ => UniTask.CompletedTask
        };
    }

    public UniTask UnloadAsync(SceneHandle handle, CancellationToken ct)
    {
        return handle.NativeHandle switch
        {
            AsyncOperationHandle<SceneInstance> => _addr.UnloadAsync(handle, ct),
            _ => _unity.UnloadAsync(handle, ct)
        };
    }
}