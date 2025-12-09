using System;

public static class ADS
{
    private static IAdsService _service;

    internal static void Bind(IAdsService service)
    {
        _service = service;
    }

    public static void ShowBanner()        => _service?.ShowBanner();
    public static void HideBanner()        => _service?.HideBanner();

    public static void ShowInterstitial()  => _service?.ShowInterstitial();
    public static bool IsReadyInterstitial => _service?.IsReadyInterstitial ?? false;

    public static void ShowRewarded(Action onReward)
        => _service?.ShowRewarded(onReward);
    public static bool IsReadyRewarded     => _service?.IsReadyRewarded ?? false;
}