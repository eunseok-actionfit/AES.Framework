using System;
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

    public AppOpenBootstrapOrchestrator(
        LifetimeScope rootScope,
        BootstrapGraph graph,
        string profile,
        SceneTransitionService scene,
        SceneTransitionBootstrapConfig cfg,
        BootstrapSettings settings)
    {
        _rootScope = rootScope;
        _graph = graph;
        _profile = profile;
        _scene = scene;
        _cfg = cfg;
        _settings = settings;
    }

    public async UniTask StartAsync(CancellationToken cancellation)
    {
        // 1) AppOpen 로딩 오버레이 표시 (프리팹 내부 LoadingOverlayUIBase가 Registry 등록)
        AppOpenLoadingOverlaySpawner spawner = null;

        if (_settings != null &&
            _settings.AppOpenLoading == AppOpenLoadingMode.Prefab &&
            _settings.AppOpenLoadingPrefab != null)
        {
            spawner = new AppOpenLoadingOverlaySpawner(_settings.AppOpenLoadingPrefab);
            spawner.Show();
        }

        // helper
        void Set(float p, string msg = null)
        {
            var ui = LoadingUIRegistry.Current;
            if (ui == null) return;

            // AppOpen은 realtime/smoothed 동일 값으로 넣는 정책
            ui.SetProgress(p, p);
            if (!string.IsNullOrEmpty(msg))
                ui.SetMessage(msg);
        }

        Set(0.02f, "Initializing...");

        const float bootMin = 0.05f;
        const float bootMax = 0.85f;

        var progress = new Progress<BootstrapProgress>(bp =>
        {
            var mapped = Mathf.Lerp(bootMin, bootMax, Mathf.Clamp01(bp.Normalized));
            Set(mapped);

            if (!string.IsNullOrEmpty(bp.FeatureId))
            {
                if (bp.Phase == BootstrapProgressPhase.FeatureBegin)
                    Set(mapped, $"Boot: {bp.FeatureId}");
                else if (bp.Phase == BootstrapProgressPhase.FeatureProgress && !string.IsNullOrEmpty(bp.Message))
                    Set(mapped, $"Boot: {bp.FeatureId} - {bp.Message}");
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

        Set(0.90f, "Entering first scene...");

        var firstKey = _settings != null ? _settings.FirstSceneKey : "Lobby";

        // 2) 첫 씬 진입은 SceneTransition로. AppOpen UI가 이미 떠 있으니 ShowLoadingScreen=false
        await _scene.RunAsync(new LoadRequest
        {
            DestinationKey = firstKey,

            ShowLoadingScreen = false,
            LoadingKeyOverride = null,

            ActivateOnLoad = true,
            ActivationGateTimeoutMs = 0,

            LoadAdditive = false,
            UnloadPolicy = UnloadPolicy.AllLoadedScenes,
            KeepSceneNames = _cfg != null
                ? new System.Collections.Generic.List<string>(_cfg.KeepSceneNames ?? Array.Empty<string>())
                : new System.Collections.Generic.List<string>(),

            EntryFadeDuration = 0f,
            ExitFadeDuration = 0f,

            UseAntiSpill = _cfg != null ? _cfg.UseAntiSpill : true,
            EnableFallback = false
        }, cancellation);

        Set(1f, "Ready");

        // 3) AppOpen 오버레이 숨김 (UI는 OnDisable에서 Registry 해제)
        spawner?.Hide();
    }
}
