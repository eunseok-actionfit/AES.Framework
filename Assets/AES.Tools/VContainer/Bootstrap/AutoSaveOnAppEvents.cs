using System;
using System.Threading;
using AES.Tools.Core;
using AES.Tools.VContainer.AppLifetime;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;


namespace AES.Tools.VContainer
{
    public sealed class AutoSaveOnAppEvents : IStartable, IDisposable
    {
        readonly ISaveCoordinator     saveCoordinator;

        CancellationTokenSource cts;

        public AutoSaveOnAppEvents(
            ISaveCoordinator saveCoordinator)
        {
            this.saveCoordinator = saveCoordinator;
        }
        
        CompositeDisposable disposables = new();
        public void Start()
        {
            cts = new CancellationTokenSource();

            new EventBinding<ApplicationFocusChangedEvent>()
                .Register()
                .Add(OnFocusChanged)
                .AddTo(disposables);
            new EventBinding<ApplicationPauseChangedEvent>().Register().AddTo(disposables);
            new EventBinding<ApplicationQuitEvent>().Register().AddTo(disposables);
        }

        public void Dispose()
        {
            disposables.Dispose();
            
            Debug.Log("[AutoSaveOnAppEvents] Dispose");

            // 여기서 "마지막 세이브" 한 번 더 시도
            SaveOnDisposeAsync().Forget();

            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        void OnFocusChanged(ApplicationFocusChangedEvent evt)
        {
            // 포커스를 잃었을 때만 저장 시도
            if (evt.Focused)
                return;
            
            Debug.Log("[AutoSaveOnAppEvents] Focus lost");

            // 포커스 아웃 저장: 스코프/씬 정리 시에는 취소되도록 토큰 사용
            SaveOnFocusLostAsync().Forget();
        }

        void OnPausedChanged(bool paused)
        {
            if (!paused)
                return;
            Debug.Log("[AutoSaveOnAppEvents] Paused");

            // 일시정지 들어갈 때: 스코프/씬 정리 시에는 취소되도록 토큰 사용
            SaveOnPauseAsync().Forget();
        }

        void OnQuit()
        {
            Debug.Log("[AutoSaveOnAppEvents] Quit");
            // 이건 실제로는 안 올 수도 있음 (Dispose가 먼저면)
            // 들어오면 보너스 한 번 더 저장
            SaveOnQuitAsync().Forget();
        }

        async UniTaskVoid SaveOnFocusLostAsync()
        {
            try
            {
                var token = cts?.Token ?? CancellationToken.None;
                await saveCoordinator.SaveAllAsync(token);
            }
            catch (OperationCanceledException)
            {
                // 스코프 정리 중이면 그냥 무시
            }
            catch (Exception e)
            {
                Debug.LogError($"[AutoSaveOnAppEvents] Focus save failed: {e}");
            }
        }

        async UniTaskVoid SaveOnPauseAsync()
        {
            try
            {
                var token = cts?.Token ?? CancellationToken.None;
                await saveCoordinator.SaveAllAsync(token);
            }
            catch (OperationCanceledException)
            {
                // 스코프 정리 중이면 그냥 무시
            }
            catch (Exception e)
            {
                Debug.LogError($"[AutoSaveOnAppEvents] Pause save failed: {e}");
            }
        }

        async UniTaskVoid SaveOnQuitAsync()
        {
            try
            {
                // Quit 세이브는 토큰 없이 끝까지 시도
                await saveCoordinator.SaveAllAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
               Debug.LogError($"[AutoSaveOnAppEvents] Quit save failed: {e}");
            }
        }

        async UniTaskVoid SaveOnDisposeAsync()
        {
            try
            {
                // 실제 보장용 마지막 세이브: Dispose에서는 무조건 None
                await saveCoordinator.SaveAllAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AutoSaveOnAppEvents] Dispose save failed: {e}");
            }
        }
    }
}
