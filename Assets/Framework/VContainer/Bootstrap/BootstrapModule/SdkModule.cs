using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;


namespace AES.Tools.VContainer
{

    namespace MyGame
    {
        [CreateAssetMenu(menuName = "Game/Bootstrap Modules/SDK Module", fileName = "SdkModule")]
        public sealed class SdkModule : BootstrapModule
        {
            [Header("광고 SDK 활성화")]
            [SerializeField]
            private bool enableAds = true;

            [Header("Analytics SDK 활성화")]
            [SerializeField]
            private bool enableAnalytics = true;

            [Header("리모트 설정(원격 Config) 활성화")]
            [SerializeField]
            private bool enableRemoteConfig = false;

            public override UniTask Initialize(LifetimeScope rootScope)
            {
                Debug.Log("[SdkModule] Initialize");

                if (enableAds)
                {
                    InitializeAds(rootScope);
                }

                if (enableAnalytics)
                {
                    InitializeAnalytics(rootScope);
                }

                if (enableRemoteConfig)
                {
                    InitializeRemoteConfig(rootScope);
                }
                return UniTask.CompletedTask;
            }

            private void InitializeAds(LifetimeScope rootScope)
            {
                Debug.Log("[SdkModule] Ads SDK init");

                // 예:
                // AdsSdk.Initialize();
                // var adsService = rootScope?.Container.Resolve<IAdsService>();
                // adsService?.Initialize();
            }

            private void InitializeAnalytics(LifetimeScope rootScope)
            {
                Debug.Log("[SdkModule] Analytics SDK init");

                // 예:
                // AnalyticsSdk.Initialize();
                // var analytics = rootScope?.Container.Resolve<IAnalyticsService>();
                // analytics?.SetUserId(...);
            }

            private void InitializeRemoteConfig(LifetimeScope rootScope)
            {
                Debug.Log("[SdkModule] Remote Config init");

                // 예:
                // RemoteConfigSdk.Initialize();
                // await RemoteConfigSdk.FetchAndActivateAsync();
            }
        }
    }

}


