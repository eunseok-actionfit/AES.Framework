using System;
using System.Collections.Generic;
using System.Threading;
using AES.Tools.SceneLoading.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;


namespace AES.Tools.SceneLoading
{
    using Addressables = UnityEngine.AddressableAssets.Addressables;

    public class AddressablesSceneLoader : ISceneLoader
    {
        // Addressables로 로드한 씬을 추적: Scene -> Handle
        private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _addrSceneHandles = new();

        public async UniTask<Scene> Load(SceneRef sceneRef, SceneLoadOptions options)
        {
            var ct = options.ExternalToken;

            if(sceneRef.IsAddressable) {
                var handle = Addressables.LoadSceneAsync(
                    sceneRef.AddressKey,
                    options.Mode == SceneLoadMode.Single ? LoadSceneMode.Single : LoadSceneMode.Additive,
                    activateOnLoad: options.AllowSceneActivation);

                await ReportProgress(handle, options.OnProgress, ct);

                // 수동 활성화 모드일 경우 별도 Activate
                if(!options.AllowSceneActivation) { await handle.Result.ActivateAsync().ToUniTask(cancellationToken: ct); }

                var inst = handle.Result; // SceneInstance
                var loaded = inst.Scene;

                // 핸들 보관 (키: Scene.name)
                _addrSceneHandles[loaded.name] = handle;

                if(options.SetActive)
                    SceneManager.SetActiveScene(loaded);

                return loaded;
            }

            if(sceneRef.IsNamed) {
                var op = SceneManager.LoadSceneAsync(
                    sceneRef.SceneName,
                    options.Mode == SceneLoadMode.Single ? LoadSceneMode.Single : LoadSceneMode.Additive);

                if(op == null)
                    throw new Exception("SceneManager.LoadSceneAsync() returned null. Is the scene in Build Settings?");

                op.allowSceneActivation = options.AllowSceneActivation;

                await ReportProgress(op, options.OnProgress, ct, options.AllowSceneActivation);

                // 수동 활성화 모드면 여기서 활성화 시킨 뒤 완료까지 대기
                if(!options.AllowSceneActivation) {
                    op.allowSceneActivation = true;
                    // 활성화 완료까지 한 프레임 이상 대기
                    while (!op.isDone)
                        await UniTask.Yield();
                }

                var loaded = SceneManager.GetSceneByName(sceneRef.SceneName);

                if(options.SetActive)
                    SceneManager.SetActiveScene(loaded);

                return loaded;
            }

            throw new ArgumentException("Invalid SceneRef: both SceneName and AddressKey are empty.");
        }

        public async UniTask Unload(Scene scene)
        {
            if(!scene.IsValid()) return;
            string name = scene.name;
            // Addressables로 로드된 씬인지 먼저 확인
            if(_addrSceneHandles.TryGetValue(name, out var handle)) {
                // Addressables 경로로 정리 (의존성 안전)
                var unload = Addressables.UnloadSceneAsync(handle);
                await unload.Task;
                _addrSceneHandles.Remove(name);
                return;
            }

            // Named(scene build) 경로
            await SceneManager.UnloadSceneAsync(scene).ToUniTask();
        }
        
        // --------- Helpers ----------

        private async static UniTask ReportProgress(AsyncOperationHandle<SceneInstance> handle,
                                                    Action<float> onProgress, CancellationToken ct)
        {
            while (!handle.IsDone) {
                ct.ThrowIfCancellationRequested();
                onProgress?.Invoke(handle.PercentComplete);
                await UniTask.Yield();
            }

            onProgress?.Invoke(1f);

            if(handle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Addressables scene load failed: {handle.OperationException}");
        }

        // allowSceneActivation=false이면 isDone이 안 오므로, 0.9까지만 기다리고 반환
        private static async UniTask ReportProgress(AsyncOperation op, Action<float> onProgress, CancellationToken ct, bool allowSceneActivation)
        {
            if(allowSceneActivation) {
                while (!op.isDone) {
                    ct.ThrowIfCancellationRequested();
                    onProgress?.Invoke(op.progress);
                    await UniTask.Yield();
                }

                onProgress?.Invoke(1f);
                return;
            }

            // 수동 활성화 모드: 0.9(=로딩 완료 직전)까지만 진척 보고 후 반환
            const float readyThreshold = 0.89f;
            while (op.progress < readyThreshold) {
                ct.ThrowIfCancellationRequested();
                onProgress?.Invoke(op.progress);
                await UniTask.Yield();
            }

            onProgress?.Invoke(0.9f);
            // 호출측에서 allowSceneActivation을 true로 전환 후 isDone을 기다리게 함
        }
    }
}