using System;
using AYellowpaper.SerializedCollections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer.Bootstrap.Framework.Features
{
    [CreateAssetMenu(menuName = "Game/Bootstrap/Features/Ads Feature", fileName = "AdsFeature")]
    public sealed class AdsFeature : AppFeatureSO
    {
        [Header("Ads 전체 ON/OFF")]
        [SerializeField] private bool enableAds = true;

        [Header("Ads Profiles")]
        [SerializeField] private AdsProfile[] profiles;

        [Header("현재 환경")]
        [SerializeField] private AdsEnvironment currentEnvironment = AdsEnvironment.Production;

        [Header("Interstitial Rules")]
        [SerializeField, Min(0f)] private float interstitialMinIntervalSeconds = 40f;
        [SerializeField, Min(0)] private int interstitialMaxPerSession = 10;

        [Header("Runtime Flags")]
        [SerializeField] private bool runtimeAdsDisabled;

        [Header("Test Device CSV")]
        [SerializeField] private TextAsset testDeviceCSV;

        [Header("Test Devices (Name -> AdId)")]
        [SerializeField] private SerializedDictionary<string, string> testDevices;
        public override void Install(IContainerBuilder builder, in FeatureContext ctx)
        {
            var settings = new AdsRuntimeSettings
            {
                interstitialMinIntervalSeconds = interstitialMinIntervalSeconds,
                interstitialMaxPerSession = interstitialMaxPerSession,
                runtimeAdsDisabled = runtimeAdsDisabled,
            };
            builder.RegisterInstance(settings);

            var flags = CreateTestDeviceFlags(testDeviceCSV);
            builder.RegisterInstance(flags);

            builder.Register<IAdsProviderFactory, AdsProviderFactory>(Lifetime.Singleton);
            builder.Register<AdsService>(Lifetime.Singleton).As<IAdsService>();
        }

        public override async UniTask Initialize(LifetimeScope rootScope, FeatureContext ctx)
        {
            if (!enableAds)
            {
                Debug.Log("[AdsFeature] Ads disabled.");
                return;
            }
            
#if AESFW_ADS_MAX
            ApplyMaxTestDevices(testDevices);


            ApplyMaxTestDevices(testDevices);

            if (!MaxSdk.IsInitialized())
            {
                Debug.Log("[AdsFeature] Initializing Max SDK...");
                MaxSdk.InitializeSdk();
            }
#endif
            var profile = GetActiveProfile();
            if (profile == null)
            {
                Debug.LogWarning("[AdsFeature] No AdsProfile found.");
                return;
            }

            var adsService = rootScope.Container.Resolve<IAdsService>();
            adsService.Configure(profile);
            ADS.Bind(adsService);

            await UniTask.CompletedTask;
        }

        private static void ApplyMaxTestDevices(SerializedDictionary<string, string> map)
        {
            if (map == null || map.Count == 0) return;

            var ids = new System.Collections.Generic.List<string>(map.Count);
            foreach (var kv in map)
            {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    ids.Add(kv.Value.Trim());
            }

            if (ids.Count > 0)
                MaxSdk.SetTestDeviceAdvertisingIdentifiers(ids.ToArray());
        }
        
        private AdsProfile GetActiveProfile()
        {
            if (profiles == null || profiles.Length == 0)
                return null;

            var platform = Application.platform;

            var match = Array.Find(profiles, p => p.environment == currentEnvironment && p.platform == platform);
            if (match != null) return match;

            match = Array.Find(profiles, p => p.environment == currentEnvironment);
            if (match != null) return match;

            match = Array.Find(profiles, p => p.platform == platform);
            if (match != null) return match;

            return profiles[0];
        }

        private static TestDeviceFlags CreateTestDeviceFlags(TextAsset csv)
        {
            var flags = new TestDeviceFlags();
            if (!csv) return flags;

            // adId -> info
            var table = TestDeviceCSVParser.ParseAdIdTable(csv.text);

            // 가능한 경우 Advertising ID 기반으로 현재 기기 매칭
            var currentAdId = TestDeviceCSVParser.GetCurrentAdvertisingIdSafe();

            if (table.TryGetValue(currentAdId, out var info))
            {
                flags.adsDisabled = info.adsDisabled;
                flags.isTester = info.isTester;
                flags.matchedName = info.name;
                flags.matchedDeviceId = info.adId;

#if UNITY_EDITOR
                Debug.Log($"[Ads] Test device matched: {info.name} ({info.adId}), adsDisabled={info.adsDisabled}");
#endif
            }

            return flags;
        }

    }
    
    
}
