using System.Linq;
using AES.Tools;
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

[CreateAssetMenu(menuName = "App/Ads/Ads Module")]
public class AdsModule : BootstrapModule
{
    [Header("광고 전체 ON/OFF")]
    public bool enableAds = true;

    // 헤더 텍스트 + 요소 라벨(환경 / 플랫폼)
    [AesLabelText("프로필 목록 (환경/플랫폼별)")]
    [AesListLabel("@environment + \" / \" + platform")]
    public AdsProfile[] profiles;

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

        var profile = GetActiveProfile();
        if (profile == null)
        {
            Debug.LogWarning("[AdsModule] No AdsProfile found.");
            return;
        }

        var adsService = rootScope.Container.Resolve<IAdsService>();
        adsService.Configure(profile); // 프로필 단위로 전달

        // Static Facade 연결
        ADS.Bind(adsService);

        await UniTask.CompletedTask;
    }
}
