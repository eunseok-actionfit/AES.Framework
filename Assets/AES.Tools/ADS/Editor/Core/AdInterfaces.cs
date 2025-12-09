using System;

public interface IAdsService
{
    void Configure(AdsProfile profile);

    void ShowBanner();
    void HideBanner();

    void ShowInterstitial();
    bool IsReadyInterstitial { get; }

    void ShowRewarded(Action onReward);
    bool IsReadyRewarded { get; }
}

public interface IAppOpenAdService
{
    void Initialize();
    void Load();
    bool IsReady { get; }
    void ShowIfReady();
}

public interface IInterstitialAdService
{
    void Initialize();
    void Load();
    bool IsReady { get; }
    bool Show();
}

public interface IRewardedAdService
{
    void Initialize();
    void Load();
    bool IsReady { get; }
    bool Show(Action onReward);
}

public interface IBannerAdService
{
    void Initialize();
    void LoadAndShow();
    void Hide();
    void Show();
}