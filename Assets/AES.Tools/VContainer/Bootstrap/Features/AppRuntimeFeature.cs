using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using AES.Tools.TimeManager.Schedulers;
using AES.Tools.VContainer.Services;
using AES.Tools.VContainer.Services.Loading;

namespace AES.Tools.VContainer.Bootstrap.Framework.Features
{
    [CreateAssetMenu(menuName = "Game/Bootstrap/Features/App Runtime Feature", fileName = "AppRuntimeFeature")]
    public sealed class AppRuntimeFeature : AppFeatureSO
    {
        [Header("Frame Rate")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool disableVSync = true;

        public override void Install(IContainerBuilder b, in FeatureContext ctx)
        {
            // ===== CoreEngineInstaller =====
            b.Register<TimerScheduler>(Lifetime.Singleton).As<ITimerScheduler>();
            b.Register<CommandHistory>(Lifetime.Singleton).As<ICommandHistory>();
            b.Register<CommandBus>(Lifetime.Singleton).As<ICommandBus>();

            // ===== AssetSystemInstaller =====
            b.Register<IAssetProvider, AddressablesAssetProvider>(Lifetime.Singleton);
            b.Register<ISceneLoader, AddressablesSceneLoader>(Lifetime.Singleton);

            // ===== UXServicesInstaller =====
            b.Register<LoadingBus>(Lifetime.Singleton).As<ILoadingBus>();
            b.RegisterBuildCallback(resolver =>
            {
                LoadingBusFacade.Instance = resolver.Resolve<ILoadingBus>();
            });
            b.Register<ILoadingService, LoadingService>(Lifetime.Singleton);

            // ===== SceneFlowInstaller =====
            b.Register<ISceneFlow, SceneFlowService>(Lifetime.Singleton);
        }

        public override UniTask Initialize(LifetimeScope rootScope, FeatureContext ctx)
        {
            if (disableVSync)
                QualitySettings.vSyncCount = 0;

            Application.targetFrameRate = targetFrameRate;

            return UniTask.CompletedTask;
        }
    }
}
