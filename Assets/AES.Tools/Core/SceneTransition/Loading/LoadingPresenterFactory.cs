public sealed class LoadingPresenterFactory
{
    private readonly SceneTransitionBootstrapConfig _cfg;
    private readonly OverlayLoadingPresenter _overlay;
    private readonly LoadingScenePresenter _scene;

    public LoadingPresenterFactory(
        SceneTransitionBootstrapConfig cfg,
        OverlayLoadingPresenter overlay,
        LoadingScenePresenter scene)
    {
        _cfg = cfg;
        _overlay = overlay;
        _scene = scene;
    }

    public ILoadingPresenter Select(LoadRequest req)
    {
        if (_cfg == null) return _overlay;
        return _cfg.LoadingMode == LoadingPresentationMode.Scene ? (ILoadingPresenter)_scene : _overlay;
    }
}