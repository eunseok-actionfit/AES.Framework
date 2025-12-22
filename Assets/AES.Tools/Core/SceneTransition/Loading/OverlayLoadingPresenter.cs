using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class OverlayLoadingPresenter : ILoadingPresenter
{
    private readonly SceneTransitionBootstrapConfig _cfg;
    private GameObject _instance;

    public OverlayLoadingPresenter(SceneTransitionBootstrapConfig cfg)
    {
        _cfg = cfg;
    }

    public UniTask ShowAsync(TransitionContext ctx, CleanupPlan plan, CancellationToken ct)
    {
        if (!ctx.Request.ShowLoadingScreen)
            return UniTask.CompletedTask;

        if (_instance != null)
            return UniTask.CompletedTask;

        if (_cfg == null || _cfg.OverlayLoadingPrefab == null)
            return UniTask.CompletedTask;

        _instance = Object.Instantiate(_cfg.OverlayLoadingPrefab);
        Object.DontDestroyOnLoad(_instance);

        // Overlay prefab 내부에 LoadingOverlayUIBase(또는 ILoadingOverlayUI 구현체)가 있으면
        // OnEnable에서 LoadingUIRegistry에 자동 등록됨.
        return UniTask.CompletedTask;
    }

    public UniTask HideAsync(TransitionContext ctx, CleanupPlan plan, CancellationToken ct)
    {
        if (_instance != null)
        {
            Object.Destroy(_instance);
            _instance = null;
        }
        return UniTask.CompletedTask;
    }
}