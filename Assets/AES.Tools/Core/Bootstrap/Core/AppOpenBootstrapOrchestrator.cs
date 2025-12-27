using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using AES.Tools.VContainer.Bootstrap.Framework;
using AES.Tools.VContainer.Bootstrap;

public sealed class AppOpenBootstrapOrchestrator : IAsyncStartable
{
    private readonly LifetimeScope _rootScope;
    private readonly BootstrapGraph _graph;
    private readonly string _profile;

    private readonly SceneTransitionService _scene;
    private readonly SceneTransitionBootstrapConfig _cfg;
    private readonly BootstrapSettings _settings;

    private readonly ILoadingProgressHub _hub;

    public AppOpenBootstrapOrchestrator(
        LifetimeScope rootScope,
        BootstrapGraph graph,
        string profile,
        SceneTransitionService scene,
        SceneTransitionBootstrapConfig cfg,
        BootstrapSettings settings,
        ILoadingProgressHub hub)
    {
        _rootScope = rootScope;
        _graph = graph;
        _profile = profile;
        _scene = scene;
        _cfg = cfg;
        _settings = settings;
        _hub = hub;
    }

    // AppOpen 전용: ITransitionEvents를 "구독 가능" 형태로 어댑팅
    private sealed class AppOpenTransitionEvents : ITransitionEvents
    {
        public event Action<TransitionStatus> OnStatus;
        public void Emit(TransitionStatus status) => OnStatus?.Invoke(status);
    }

    public async UniTask StartAsync(CancellationToken cancellation)
    {
        AppOpenLoadingOverlaySpawner spawner = null;

        if (_settings != null &&
            _settings.AppOpenLoading == AppOpenLoadingMode.Prefab &&
            _settings.AppOpenLoadingPrefab != null)
        {
            spawner = new AppOpenLoadingOverlaySpawner(_settings.AppOpenLoadingPrefab);
            spawner.Show();
        }

        _hub?.SetMessage("Initializing...");
        _hub?.ReportRealtime01(0.02f);

        const float bootMin = 0.05f;
        const float bootMax = 0.85f;

        using (_hub.PushRange(bootMin, bootMax))
        {
            var progress = new Progress<BootstrapProgress>(bp =>
            {
                _hub?.ReportRealtime01(Mathf.Clamp01(bp.Normalized));

                if (!string.IsNullOrEmpty(bp.FeatureId))
                {
                    if (bp.Phase == BootstrapProgressPhase.FeatureBegin)
                        _hub?.SetMessage($"Boot: {bp.FeatureId}");
                    else if (bp.Phase == BootstrapProgressPhase.FeatureProgress && !string.IsNullOrEmpty(bp.Message))
                        _hub?.SetMessage($"Boot: {bp.FeatureId} - {bp.Message}");
                }
            });

            if (_graph)
            {
                await BootstrapRunner.InitializeAllAsync(
                    _graph,
                    _profile,
                    _rootScope,
                    Application.platform,
#if UNITY_EDITOR
                    true
#else
                    false
#endif
                    ,
                    progress
                );
            }
        }

        //  1) Ready를 먼저 찍고
        _hub?.SetMessage("Ready");
        _hub?.ReportRealtime01(1f);

        //  2) 스무스가 진짜 1.0 될 때까지 기다린 뒤
        await _hub.WaitUntilFilledAsync(cancellation);

        //  3) 그 다음 씬 전환 시작
        _hub?.SetMessage("Entering first scene...");

        var firstKey = _settings != null ? _settings.FirstSceneKey : "Lobby";

        // ExitFade(검정->밝음) 시작 시점에 AppOpen 오버레이를 내려서 로비가 보이게 함
        var events = new AppOpenTransitionEvents();
        bool spawnerHidden = false;

        void HideSpawner()
        {
            if (spawnerHidden) return;
            spawnerHidden = true;
            spawner?.Hide();
        }

        events.OnStatus += status =>
        {
            if (status == TransitionStatus.ExitFade)
                HideSpawner();
        };

        await _scene.RunAsync(new LoadRequest
        {
            DestinationKey = firstKey,

            ShowLoadingScreen = false,
            LoadingKeyOverride = null,

            Events = events,

            ActivateOnLoad = true,
            ActivationGateTimeoutMs = 0,

            LoadAdditive = false,
            UnloadPolicy = UnloadPolicy.AllLoadedScenes,
            KeepSceneNames = _cfg != null
                ? new List<string>(_cfg.KeepSceneNames ?? Array.Empty<string>())
                : new List<string>(),

            UseAntiSpill = _cfg?.UseAntiSpill ?? true,
            EnableFallback = false,

            SmoothedProgress = 0f
        }, cancellation);

        // 안전장치: ExitFade가 없거나 이벤트가 안 오면 여기서라도 내림
        HideSpawner();

        _hub.Stop();
    }
}
