using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

public enum GateId { AfterUnload, BeforeActivation }
public enum UnloadPolicy { None, ActiveScene, AllLoadedScenes }

public sealed class SceneTransitionService
{
    private readonly ISceneLoader _loader;
    private readonly IGates _gates;
    private readonly IContentCache _cache;
    private readonly SceneTransitionBootstrapConfig _cfg;
    private readonly ISceneArgsCarrier _sceneArgs;

    private readonly SceneCatalog _sceneCatalog;
    private readonly LoadingCatalog _loadingCatalog;
    private readonly LifetimeScope _rootScope;

    private readonly TransitionViewModel _vm;

    // NEW: presenter 선택 팩토리(씬/오버레이 전략)
    private readonly LoadingPresenterFactory _loadingPresenterFactory;

    private readonly FallbackGuard _fallbackGuard = new();
    private CancellationTokenSource _inflightCts;

    private LoadRequest _lastRequest;
    private bool _vmBound;

    public SceneTransitionService(
        ISceneLoader loader,
        IGates gates,
        IContentCache cache,
        SceneTransitionBootstrapConfig cfg,
        LifetimeScope rootScope,
        ISceneArgsCarrier sceneArgs,
        SceneCatalog sceneCatalog,
        LoadingCatalog loadingCatalog,
        LoadingPresenterFactory loadingPresenterFactory,
        TransitionViewModel vm
    )
    {
        _loader = loader;
        _gates = gates;
        _cache = cache;
        _cfg = cfg;
        _rootScope = rootScope;
        _sceneArgs = sceneArgs;
        _sceneCatalog = sceneCatalog;
        _loadingCatalog = loadingCatalog;
        _loadingPresenterFactory = loadingPresenterFactory;
        _vm = vm;
    }

    public void CancelCurrent() => _inflightCts?.Cancel();

    public async UniTask RunAsync(LoadRequest request, CancellationToken externalCt)
    {
        _lastRequest = request;

        // Volatile args for next scene
        if (request.SceneArgs != null) _sceneArgs.Set(request.SceneArgs);
        else _sceneArgs.Clear();

        // Bind VM commands only once
        if (!_vmBound && _vm != null)
        {
            _vm.RetryRequested += () => Retry().Forget();
            _vm.ClearCacheRequested += () => ClearCacheAndRetry().Forget();
            _vmBound = true;
        }

        await RunInternal(request, externalCt);
    }

    // LoadingScreenKey 확정 (override ?? default)
    private LoadingScreenKey ResolveLoadingKey(LoadRequest req)
    {
        // override key name -> LoadingCatalog
        if (!string.IsNullOrEmpty(req.LoadingKeyOverride) &&
            _loadingCatalog != null &&
            _loadingCatalog.TryResolve(req.LoadingKeyOverride, out var k))
            return k;

        // default
        return _cfg != null ? _cfg.DefaultLoading : default;
    }

    private async UniTask RunInternal(LoadRequest request, CancellationToken externalCt)
    {
        _inflightCts?.Cancel();
        _inflightCts?.Dispose();
        _inflightCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        var ct = _inflightCts.Token;

        _vm?.ResetForNewRun();
        _vm?.SetStatus(TransitionStatus.LoadStarted);
        _vm?.SetRetryVisible(false);
        _vm?.SetClearCacheVisible(false);

        var plan = new CleanupPlan
        {
            PreviousActiveScene = SceneManager.GetActiveScene(),
            RestorePreviousActiveScene = true
        };

        // NOTE:
        // 네 프로젝트에서 TransitionContext가 LoadingKey/LoadingPresenter를 포함하도록 확장되어 있다면 그대로 사용.
        // 포함되어 있지 않다면, 이 2줄(LoadingKey/LoadingPresenter)은 TransitionContext에 필드 추가가 필요.
        var ctx = new TransitionContext
        {
            Request = request,
            Loader = _loader,
            Gates = _gates,

            LoadingKey = ResolveLoadingKey(request),
            LoadingPresenter = _loadingPresenterFactory != null ? _loadingPresenterFactory.Select(request) : null
        };

        var antiSpill = request.UseAntiSpill ? new AntiSpill() : null;

        float lastRealtime = 0f;
        float lastSmoothed = 0f;

        var realtimeSink = new Progress<float>(p => { lastRealtime = Mathf.Clamp01(p); });

        var smoothedSink = new Progress<float>(p =>
        {
            lastSmoothed = Mathf.Clamp01(p);

            // Registry 기반 UI (씬/오버레이 공통)
            LoadingUIRegistry.Current?.SetProgress(lastRealtime, lastSmoothed);

            // MVVM
            _vm?.SetProgress(lastRealtime, lastSmoothed);
        });

        using var smoother = new ProgressSmoother(
            request.ProgressSpeedFn ?? (_ => 5f),
            realtimeSink,
            smoothedSink);

        var progressProxy = new Progress<float>(p => smoother.SetRealtime(p));

        var tick = UniTask.Create(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                smoother.Tick(Time.unscaledDeltaTime);
                await UniTask.Yield();
            }
        });

        bool failed = false;
        Exception failure = null;

        try
        {
            request.Events?.Emit(TransitionStatus.LoadStarted);

            var pipe = new TransitionPipeline()
                .Add(new BlockInputStep(request.InputBlocker))

                // Presenter 기반 로딩 표시(씬/오버레이)
                .Add(new LoadLoadingScreenStep(plan, _loadingPresenterFactory))

                .Add(new DelayStep(request.BeforeEntryFadeDelay, TransitionStatus.BeforeEntryFade))
                .Add(new FadeInStep(request.Fader, request.EntryFadeDuration, TransitionStatus.EntryFade))
                .Add(new DelayStep(request.AfterEntryFadeDelay, TransitionStatus.AfterEntryFade));

            if (request.UseAntiSpill)
                pipe.Add(new AntiSpillPrepareStep(antiSpill, plan, request.AntiSpillSceneName));

            pipe.Add(new UnloadScenesStep())
                .Add(new GateStep(GateId.AfterUnload))
                .Add(new DelayStep(request.BeforeActivationDelay, TransitionStatus.BeforeSceneActivation))
                .Add(new LoadSceneStepWithResolver(progressProxy, plan, _sceneCatalog, _rootScope))
                .Add(new EmitStatusStep(TransitionStatus.WaitingForServer, null));

            _vm?.SetStatus(TransitionStatus.WaitingForServer);

            if (request.ActivationGateTimeoutMs > 0)
                pipe.Add(new TimedGateStep(request.ActivationGate, request.ActivationGateTimeoutMs));
            else
                pipe.Add(new GateStep(request.ActivationGate));

            pipe.Add(new ActivateSceneStep())
                .Add(new SetActiveSceneStepWithResolver());

            if (request.UseAntiSpill)
                pipe.Add(new AntiSpillFlushStep(antiSpill, unloadSpillScene: true));

            pipe.Add(new DelayStep(request.AfterActivationDelay, TransitionStatus.AfterSceneActivation))
                .Add(new FadeOutStep(request.Fader, request.ExitFadeDuration, TransitionStatus.ExitFade))

                // Presenter 기반 로딩 숨김
                .Add(new UnloadLoadingScreenStep(plan))

                .Add(new UnblockInputStep(request.InputBlocker));

            await pipe.Run(ctx, ct);

            request.Events?.Emit(TransitionStatus.Complete);
            _vm?.SetStatus(TransitionStatus.Complete);
            _vm?.SetMessage("Complete");
        }
        catch (Exception e)
        {
            failed = true;
            failure = e;
            throw;
        }
        finally
        {
            _inflightCts?.Cancel();
            await tick.SuppressCancellationThrow();

            if (failed)
            {
                try { _sceneArgs.Clear(); } catch { }
                try { await new CleanupStep(plan, force: true).Execute(ctx, CancellationToken.None); } catch { }
                try { request.InputBlocker?.Unblock(); } catch { }

                var code = TransitionFailureClassifier.Classify(failure);
                var policy = FailurePolicies.Get(code);

                // FailurePolicy 실제 필드명에 맞춘 매핑
                _vm?.SetStatus(TransitionStatus.Failed);
                _vm?.SetRetryVisible(policy.SuggestRetry);
                _vm?.SetClearCacheVisible(policy.ClearCacheSuggestion);
                _vm?.SetMessage(policy.UiMessageKey ?? "FAILED");

                if (policy.DoFallback && request.EnableFallback && _fallbackGuard.TryEnter())
                {
                    try { await RunFallback(request, externalCt); } catch { }
                }
            }
        }
    }

    private async UniTask RunFallback(LoadRequest original, CancellationToken externalCt)
    {
        var fb = new LoadRequest
        {
            LoadAdditive = false,
            UnloadPolicy = UnloadPolicy.AllLoadedScenes,

            ShowLoadingScreen = original.FallbackShowLoadingScreen,
            LoadingKeyOverride = original.LoadingKeyOverride,

            ActivateOnLoad = true,
            ActivationGate = GateId.BeforeActivation,
            ActivationGateTimeoutMs = 0,

            UseAntiSpill = original.FallbackUseAntiSpill,
            EnableFallback = false,

            // NOTE: UI는 이제 Service에서 직접 안 씀. 남겨도 무해.
            UI = original.UI,
            Events = original.Events,
            InputBlocker = original.InputBlocker,
            Fader = original.Fader,

            EntryFadeDuration = 0f,
            ExitFadeDuration = 0f,

            DestinationKey = original.FallbackDestinationKey
        };

        await RunInternal(fb, externalCt);
    }

    private async UniTaskVoid Retry()
    {
        if (_lastRequest == null) return;
        await RunAsync(_lastRequest, CancellationToken.None);
    }

    private async UniTaskVoid ClearCacheAndRetry()
    {
        if (_cache != null && _lastRequest != null)
        {
            try
            {
                _vm?.SetStatus(TransitionStatus.CleaningCache);
                _vm?.SetMessage("Cleaning cache...");

                switch (_lastRequest.CacheClearMode)
                {
                    case CacheClearMode.All:
                        await _cache.ClearAllAsync(CancellationToken.None);
                        break;

                    case CacheClearMode.DependencyOnly:
                    case CacheClearMode.DependencyThenClean:
                    default:
                    {
                        var key = CacheKeyResolver.ResolveForCacheKey(_lastRequest.DestinationKey, _lastRequest.CacheClearLabel);
                        await _cache.ClearByKeyAsync(key, CancellationToken.None);

                        if (_lastRequest.CacheClearMode == CacheClearMode.DependencyThenClean)
                            await _cache.CleanUnusedAsync(CancellationToken.None);

                        break;
                    }
                }
            }
            catch
            {
                try { await _cache.ClearAllAsync(CancellationToken.None); } catch { }
            }
        }

        Retry().Forget();
    }
}
