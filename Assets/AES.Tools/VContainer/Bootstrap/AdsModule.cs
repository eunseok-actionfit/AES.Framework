using System.Linq;
using AYellowpaper.SerializedCollections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AES.Tools.VContainer.Bootstrap
{
    [CreateAssetMenu(menuName = "App/Ads/Ads Module")]
    public class AdsModule : BootstrapModule
    {
        [Header("광고 전체 ON/OFF")]
        public bool enableAds = true;
        
        public AdsProfile[] profiles;
        
        [Header("MAX Test Devices")]
        [SerializedDictionary("Device Owner", "Device Ad-ID")]
        [SerializeField] private SerializedDictionary<string, string> _testDeviceMap;

        // CSV는 에디터에서 버튼으로 맵핑할 때만 사용
        [SerializeField] private TextAsset _testDeviceCsv;

        [Header("현재 환경 (에디터/런타임에서 설정)")]
        public AdsEnvironment currentEnvironment = AdsEnvironment.Production;

        public AdsProfile GetActiveProfile()
        {
            if (profiles == null || profiles.Length == 0)
                return null;

            var platform = Application.platform;

            // 1순위: 환경 + 플랫폼 둘 다 매칭
            var match = profiles.FirstOrDefault(p =>
                p.environment == currentEnvironment &&
                p.platform == platform);

            if (match != null)
                return match;

            // 2순위: 환경만 맞는 것
            match = profiles.FirstOrDefault(p =>
                p.environment == currentEnvironment);

            if (match != null)
                return match;

            // 3순위: 플랫폼만 맞는 것
            match = profiles.FirstOrDefault(p =>
                p.platform == platform);

            if (match != null)
                return match;

            // 4순위: 그냥 첫 번째
            return profiles[0];
        }

        public override async UniTask Initialize(LifetimeScope rootScope)
        {
            if (!enableAds)
            {
                Debug.Log("[AdsModule] Ads disabled.");
                return;
            }
            
            if (!MaxSdk.IsInitialized())
            {
                Debug.Log("[AdsModule] Initializing Max SDK...");

                // 에디터에서 CSV → _testDeviceMap 맵핑이 끝났다고 가정하고,
                // 여기서는 _testDeviceMap 값만 그대로 사용
                if (_testDeviceMap != null && _testDeviceMap.Count > 0)
                {
                    var ids = _testDeviceMap
                        .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                        .Select(kv => kv.Value)
                        .Distinct()
                        .ToArray();

                    if (ids.Length > 0)
                    {
                        MaxSdk.SetTestDeviceAdvertisingIdentifiers(ids);
                        Debug.Log($"[AdsModule] MAX Test Devices Registered: {ids.Length}");
                    }
                }

                MaxSdkCallbacks.OnSdkInitializedEvent += _ =>
                {
                    Debug.Log("[AdsModule] Max SDK initialized.");
                };

                MaxSdk.InitializeSdk();
            }

            var profile = GetActiveProfile();
            if (profile == null)
            {
                Debug.LogWarning("[AdsModule] No AdsProfile found.");
                return;
            }

            var adsService = rootScope.Container.Resolve<IAdsService>();
            adsService.Configure(profile);

            ADS.Bind(adsService);

            await UniTask.CompletedTask;
        }
    }
}
