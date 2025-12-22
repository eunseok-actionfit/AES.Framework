using AES.Tools.VContainer.Bootstrap;
using AES.Tools.VContainer.Bootstrap.Framework;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AES.Tools.VContainer.Scope
{
    public sealed class AppLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // 1) BootstrapSettings 로드 (Root 생성 시 이미 Instance가 잡혀 있음)
            var settings = BootstrapSettings.Instance;
            if (settings == null)
                settings = Resources.Load<BootstrapSettings>("BootstrapSettings");

            if (settings == null)
            {
                Debug.LogError("[AppLifetimeScope] BootstrapSettings not found.");
                return;
            }

            // 2) Feature 설치 (graph/profile은 Settings에서)
            BootstrapRunner.InstallAll(
                settings.Graph,
                settings.Profile,
                builder,
                Application.platform,
#if UNITY_EDITOR
                true
#else
                false
#endif
            );

            // 3) Settings/Graph/Profile을 DI로 제공
            builder.RegisterInstance(settings);
            builder.RegisterInstance(settings.Graph);
            builder.RegisterInstance(settings.Profile);

            // 4) AppOpen Orchestrator 실행
            builder.RegisterEntryPoint<AppOpenBootstrapOrchestrator>(Lifetime.Singleton);
        }
    }
}