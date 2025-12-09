using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;


[CreateAssetMenu(menuName = "Game/Bootstrap Modules/SDK Module", fileName = "SdkModule")]
public sealed class SdkModule : BootstrapModule
{
    [Header("Analytics SDK 활성화")]
    [SerializeField]
    private bool enableAnalytics = true;

    [Header("리모트 설정(원격 Config) 활성화")]
    [SerializeField]
    private bool enableRemoteConfig = false;

    public override UniTask Initialize(LifetimeScope rootScope)
    {
        Debug.Log("[SdkModule] Initialize");

        if (enableAnalytics)
            InitializeAnalytics(rootScope);

        if (enableRemoteConfig)
            InitializeRemoteConfig(rootScope);

        return UniTask.CompletedTask;
    }

    private void InitializeAnalytics(LifetimeScope rootScope)
    {
        Debug.Log("[SdkModule] Analytics SDK init");
        // AnalyticsSdk.Initialize();
        // var analytics = rootScope?.Container.Resolve<IAnalyticsService>();
        // analytics?.SetUserId(...);
    }

    private void InitializeRemoteConfig(LifetimeScope rootScope)
    {
        Debug.Log("[SdkModule] Remote Config init");
        // RemoteConfigSdk.Initialize();
        // await RemoteConfigSdk.FetchAndActivateAsync();
    }
}