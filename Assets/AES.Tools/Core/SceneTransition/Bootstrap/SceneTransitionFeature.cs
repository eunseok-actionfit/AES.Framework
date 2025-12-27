using System;
using System.Linq;
using AES.Tools.VContainer.Bootstrap.Framework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

[Serializable]
[CreateAssetMenu(menuName = "Game/Bootstrap/Features/Scene Transition Feature", fileName = "SceneTransitionFeature")]
public sealed class SceneTransitionFeature : AppFeatureSO
{
    [Header("Bootstrap UX (optional)")]
    [Tooltip("Optional. If set, instantiated as Singleton and registered as SceneTransitionUxRoot.")]
    [SerializeField] private SceneTransitionUxRoot uxRootPrefab;

    [Header("Catalogs")]
    [SerializeField] private SceneCatalog sceneCatalog;
    [SerializeField] private LoadingCatalog loadingCatalog;

    [Header("Default Loading (LoadingCatalog Key)")]
    [SerializeField] private string defaultLoadingKey = "LoadingInGame";

    [Header("Loading Presentation")]
    [SerializeField] private LoadingPresentationMode loadingMode = LoadingPresentationMode.Overlay;

    [Tooltip("Used when LoadingMode=Overlay. Prefab must contain a component implementing ILoadingOverlayUI (recommended: LoadingOverlayUIBase).")]
    [SerializeField] private GameObject overlayLoadingPrefab;

    [Header("Keep Scenes (unload 제외 대상)")]
    [SerializeField] private string[] keepSceneNames = { "Persistent" };

    [Header("Defaults")]
    [SerializeField] private bool useAntiSpill = true;
    [SerializeField] private float entryFadeDuration = 0f;
    [SerializeField] private float afterEntryFadeDelay = 0f;
    [SerializeField] private float exitFadeDuration = 0f;

    public override void Install(IContainerBuilder builder, in FeatureContext ctx)
    {
        // Config
        var cfg = BuildConfig();
        builder.RegisterInstance(cfg);

        // Catalogs
        builder.RegisterInstance(sceneCatalog);
        builder.RegisterInstance(loadingCatalog);

        builder.Register<TransitionViewModel>(Lifetime.Singleton);
        
        // Args carrier (volatile)
        builder.Register<SceneArgsCarrier>(Lifetime.Singleton).As<ISceneArgsCarrier>();

        // Gates
        builder.Register<Gates>(Lifetime.Singleton).As<IGates>();

        // Loader stack
        builder.Register<UnitySceneLoader>(Lifetime.Singleton);
        builder.Register<AddressablesSceneLoader>(Lifetime.Singleton);
        builder.Register<HybridSceneLoader>(Lifetime.Singleton).As<ISceneLoader>();

        // Cache (optional but recommended)
        builder.Register<AddressablesContentCache>(Lifetime.Singleton).As<IContentCache>();

        // Loading presentation (strategy)
        builder.Register<OverlayLoadingPresenter>(Lifetime.Singleton).As<ILoadingPresenter>().AsSelf();
        builder.Register<LoadingScenePresenter>(Lifetime.Singleton);
        builder.Register<LoadingPresenterFactory>(Lifetime.Singleton);
        builder.Register<LoadingProgressHub>(Lifetime.Singleton).As<ILoadingProgressHub>().AsSelf();
        
        
        // Service
        builder.Register<SceneTransitionService>(Lifetime.Singleton);

        // UX root prefab (optional)
        if (uxRootPrefab)
            builder.RegisterComponentInNewPrefab(uxRootPrefab, Lifetime.Singleton);
    }

    private SceneTransitionBootstrapConfig BuildConfig()
    {
        var cfg = new SceneTransitionBootstrapConfig
        {
            LoadingMode = loadingMode,
            OverlayLoadingPrefab = overlayLoadingPrefab,
            EntryFadeDuration = entryFadeDuration,
            AfterEntryFadeDelay = afterEntryFadeDelay,
            ExitFadeDuration = exitFadeDuration,
            UseAntiSpill = useAntiSpill
        };

        // Default loading from catalog if possible
        if (loadingCatalog != null && loadingCatalog.TryResolve(defaultLoadingKey, out var key))
            cfg.DefaultLoading = key;
        else
            cfg.DefaultLoading = new LoadingScreenKey("LoadingInGame", null, false);

        if (keepSceneNames != null && keepSceneNames.Length > 0)
            cfg.KeepSceneNames = keepSceneNames.Distinct().ToArray();

        return cfg;
    }
}
