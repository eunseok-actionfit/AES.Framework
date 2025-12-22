using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using VContainer.Unity;


public interface ISceneLoader
{
    UniTask UnloadScenesAsync(IReadOnlyList<Scene> scenes, CancellationToken ct);

    UniTask<SceneHandle> LoadSceneAsync(
        ResolvedSceneKey key,
        bool additive,
        bool activateOnLoad,
        IProgress<float> progress,
        LifetimeScope parentScope,
        CancellationToken ct);

    UniTask ActivateAsync(SceneHandle handle, CancellationToken ct);
    UniTask UnloadAsync(SceneHandle handle, CancellationToken ct);
}