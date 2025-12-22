using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;


public sealed class UnitySceneLoader : ISceneLoader
{
    public async UniTask UnloadScenesAsync(IReadOnlyList<Scene> scenes, CancellationToken ct)
    {
        foreach (var s in scenes)
        {
            if (!s.IsValid() || !s.isLoaded) continue;
            
            if (SceneManager.sceneCount <= 1)
                return;
            
            var op = SceneManager.UnloadSceneAsync(s);
            if (op == null) continue;
            while (!op.isDone) { ct.ThrowIfCancellationRequested(); await UniTask.Yield(); }
        }
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
            var mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var op = SceneManager.LoadSceneAsync(key.ForUnity, mode);
            op.allowSceneActivation = activateOnLoad;

            while (op.progress < 0.9f)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(op.progress / 0.9f);
                await UniTask.Yield();
            }

            progress?.Report(1f);

            var name = Path.GetFileNameWithoutExtension(key.ForUnity);
            var scene = SceneManager.GetSceneByName(name);
            return new SceneHandle(key.ForUnity, scene, op);
        }
    }

    public async UniTask ActivateAsync(SceneHandle handle, CancellationToken ct)
    {
        if (handle.NativeHandle is not AsyncOperation op) return;

        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }
    }

    public async UniTask UnloadAsync(SceneHandle handle, CancellationToken ct)
    {
        if (!handle.Scene.IsValid() || !handle.Scene.isLoaded) return;
        
        if (SceneManager.sceneCount <= 1)
            return;
        
        var op = SceneManager.UnloadSceneAsync(handle.Scene);
        while (op != null && !op.isDone)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }
    }
}
