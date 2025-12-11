using AES.Tools.VContainer;
using AES.Tools.VContainer.Installer;
using UnityEngine;
using VContainer;

[CreateAssetMenu(menuName = "Game/Installers/Ads Installer", fileName = "AdsInstaller")]
public sealed class AdsInstaller : ScriptableInstaller
{
    [Header("Ads 런타임 설정")]
    [SerializeField] private AdsRuntimeConfig adsRuntimeConfig;

    public override void Install(IContainerBuilder builder)
    {
        // RuntimeConfig 인스턴스 등록
        builder.RegisterInstance(adsRuntimeConfig);
        
        var flags = CreateTestDeviceFlags(adsRuntimeConfig);
        builder.RegisterInstance(flags);

        // AdsService 등록 (생성자에서 ITimerScheduler, AdsRuntimeConfig 자동 주입)
        builder.Register<AdsService>(Lifetime.Singleton)
            .As<IAdsService>();
    }
    
    private TestDeviceFlags CreateTestDeviceFlags(AdsRuntimeConfig config)
    {
        var flags = new TestDeviceFlags();

        if (config == null || config.testDeviceCSV == null)
            return flags;

        var table = TestDeviceCSVParser.Parse(config.testDeviceCSV.text);

#if UNITY_ANDROID || UNITY_IOS
        string deviceId = SystemInfo.deviceUniqueIdentifier;
#else
    string deviceId = "EDITOR_DEVICE";
#endif

        if (table.TryGetValue(deviceId, out var info))
        {
            flags.adsDisabled    = info.adsDisabled;
            flags.isTester       = info.isTester;
            flags.matchedName    = info.name;
            flags.matchedDeviceId = info.deviceId;

            Debug.Log($"[Ads] Test device matched: {info.name} ({info.deviceId}), adsDisabled={info.adsDisabled}, isTester={info.isTester}");
        }
        else
        {
            Debug.Log($"[Ads] No test device match. deviceId={deviceId}");
        }

        return flags;
    }
}