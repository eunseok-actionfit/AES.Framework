#if AESFW_ADS_ADMOB || AESFW_ADS_MAX
using AES.Tools.TimeManager.Schedulers;

namespace AES.Tools.VContainer
{
    public sealed class AdsProviderFactory : IAdsProviderFactory
    {
        public IAppOpenAdService CreateAppOpen(AdsProfile cfg)
        {
            switch (cfg.appOpen.network)
            {
#if AESFW_ADS_ADMOB
                case AdNetworkType.AdMob:
                    return new AdmobAppOpenService(cfg.appOpen.adUnitId);
#endif
#if AESFW_ADS_MAX
                case AdNetworkType.AppLovinMax:
                    return new MaxAppOpenService(cfg.appOpen.adUnitId);
#endif
                default:
                    return null;
            }
        }

        public IInterstitialAdService CreateInterstitial(AdsProfile cfg, ITimerScheduler scheduler)
        {
            switch (cfg.interstitial.network)
            {
#if AESFW_ADS_ADMOB
                case AdNetworkType.AdMob:
                    return new AdmobInterstitialService(cfg.interstitial.adUnitId);
#endif
#if AESFW_ADS_MAX
                case AdNetworkType.AppLovinMax:
                    return new MaxInterstitialService(cfg.interstitial.adUnitId, scheduler);
#endif
                default:
                    return null;
            }
        }

        public IRewardedAdService CreateRewarded(AdsProfile cfg, ITimerScheduler scheduler)
        {
            switch (cfg.rewarded.network)
            {
#if AESFW_ADS_ADMOB
                case AdNetworkType.AdMob:
                    return new AdmobRewardedService(cfg.rewarded.adUnitId);
#endif
#if AESFW_ADS_MAX
                case AdNetworkType.AppLovinMax:
                    return new MaxRewardedService(cfg.rewarded.adUnitId, scheduler);
#endif
                default:
                    return null;
            }
        }

        public IBannerAdService CreateBanner(AdsProfile cfg)
        {
            switch (cfg.banner.network)
            {
#if AESFW_ADS_ADMOB
                case AdNetworkType.AdMob:
                    return new AdmobBannerService(cfg.banner.adUnitId);
#endif
#if AESFW_ADS_MAX
                case AdNetworkType.AppLovinMax:
                    return new MaxBannerService(cfg.banner.adUnitId);
#endif
                default:
                    return null;
            }
        }
    }
}
#endif
