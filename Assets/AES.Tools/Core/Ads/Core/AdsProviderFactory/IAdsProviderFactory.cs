using AES.Tools.TimeManager.Schedulers;


namespace AES.Tools.VContainer
{
    public interface IAdsProviderFactory
    {
        IAppOpenAdService CreateAppOpen(AdsProfile cfg);
        IInterstitialAdService CreateInterstitial(AdsProfile cfg, ITimerScheduler scheduler);
        IRewardedAdService CreateRewarded(AdsProfile cfg, ITimerScheduler scheduler);
        IBannerAdService CreateBanner(AdsProfile cfg);
    }
}