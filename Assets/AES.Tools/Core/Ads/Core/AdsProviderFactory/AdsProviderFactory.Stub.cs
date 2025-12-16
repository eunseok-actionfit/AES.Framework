#if !(AESFW_ADS_ADMOB || AESFW_ADS_MAX)
namespace AES.Tools.VContainer
{
    public sealed class AdsProviderFactory : IAdsProviderFactory
    {
        public IAppOpenAdService CreateAppOpen(AdsProfile cfg) => null;
        public IInterstitialAdService CreateInterstitial(AdsProfile cfg, ITimerScheduler scheduler) => null;
        public IRewardedAdService CreateRewarded(AdsProfile cfg, ITimerScheduler scheduler) => null;
        public IBannerAdService CreateBanner(AdsProfile cfg) => null;
    }
}
#endif