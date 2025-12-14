using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    [CreateAssetMenu(menuName="Bootstrap/Features/SDK Init", fileName="SdkInitFeature")]
    public sealed class SdkInitFeature : AppFeatureSO
    {
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableRemoteConfig = false;

        public override async UniTask Initialize(LifetimeScope rootScope,  FeatureContext ctx)
        {
            Debug.Log("[SdkInitFeature] Initialize");

            if (enableAnalytics && ctx.Capabilities.TryGetCapability<IAnalyticsBootstrap>(out var a))
                a.Initialize();

            if (enableRemoteConfig && ctx.Capabilities.TryGetCapability<IRemoteConfigBootstrap>(out var r))
                await r.InitializeAsync();
        }
    }
}