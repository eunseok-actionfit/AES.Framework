using System;
using AES.Tools;
using AES.Tools.VContainer.AppLifetime;
using UnityEngine;

public enum AdsServiceState
{
    Uninitialized,
    Configured,
    Disposed
}

public class AdsService : IAdsService
{
    private AdsServiceState _state = AdsServiceState.Uninitialized;

    private AdsProfile _profile;
    private EventBinding<ApplicationFocusChangedEvent> _focusBinding;
    private EventBinding<ApplicationQuitEvent> _quitBinding;
    
    private IAppOpenAdService _appOpen;
    private IInterstitialAdService _interstitial;
    private IRewardedAdService _rewarded;
    private IBannerAdService _banner;
    
    public bool IsReadyInterstitial => _interstitial?.IsReady ?? false;
    public bool IsReadyRewarded     => _rewarded?.IsReady ?? false;

    public void Configure(AdsProfile profile)
    {
        if (_state == AdsServiceState.Disposed)
        {
            Debug.LogWarning("[AdsService] Already disposed. Configure ignored.");
            return;
        }

        if (_state == AdsServiceState.Configured)
        {
            Debug.Log("[AdsService] Already configured. Skipping reconfigure.");
            return;
        }

        _profile = profile;
        _state = AdsServiceState.Configured;

        SetupProviders();
        SetupFocusBinding();
        SetupQuitBinding();
    }

    private void SetupProviders()
    {
        _appOpen      = CreateAppOpenService(_profile);
        _interstitial = CreateInterstitialService(_profile);
        _rewarded     = CreateRewardedService(_profile);
        _banner       = CreateBannerService(_profile);

        _appOpen?.Initialize();
        _appOpen?.Load();

        _interstitial?.Initialize();
        _interstitial?.Load();

        _rewarded?.Initialize();
        _rewarded?.Load();

        _banner?.Initialize();
        _banner?.LoadAndShow();
    }

    private void SetupFocusBinding()
    {
        // ApplicationLifetimeAdapter 가 포커스 이벤트를 EventBus로 쏴 줌
        _focusBinding = new EventBinding<ApplicationFocusChangedEvent>()
            .Add(e =>
            {
                if (e.Focused)
                    _appOpen?.ShowIfReady();
            })
            .Register();
    }

    private void SetupQuitBinding()
    {
        _quitBinding = new EventBinding<ApplicationQuitEvent>()
            .Add(Dispose)
            .Register();
    }

    public void Dispose()
    {
        if (_state == AdsServiceState.Disposed)
            return;

        _state = AdsServiceState.Disposed;

        _focusBinding?.Deregister();
        _focusBinding = null;

        _quitBinding?.Deregister();
        _quitBinding = null;

        (_appOpen as IDisposable)?.Dispose();
        (_interstitial as IDisposable)?.Dispose();
        (_rewarded as IDisposable)?.Dispose();
        (_banner as IDisposable)?.Dispose();

        _appOpen = null;
        _interstitial = null;
        _rewarded = null;
        _banner = null;
    }

    #region Service factories

    private IAppOpenAdService CreateAppOpenService(AdsProfile cfg)
    {
        switch (cfg.appOpen.network)
        {
            case AdNetworkType.AdMob:
                return new AdmobAppOpenService(cfg.appOpen.adUnitId);

            case AdNetworkType.AppLovinMax:
                 return new MaxAppOpenService(cfg.appOpen.adUnitId);
            case AdNetworkType.None:
            default:
                return null;
        }
    }

    private IInterstitialAdService CreateInterstitialService(AdsProfile cfg)
    {
        switch (cfg.interstitial.network)
        {
            case AdNetworkType.AdMob:
                 return new AdmobInterstitialService(cfg.interstitial.adUnitId);

            case AdNetworkType.AppLovinMax:
                 return new MaxInterstitialService(cfg.interstitial.adUnitId);
                return null;

            case AdNetworkType.None:
            default:
                return null;
        }
    }

    private IRewardedAdService CreateRewardedService(AdsProfile cfg)
    {
        switch (cfg.interstitial.network)
        {
            case AdNetworkType.AdMob:
                 return new AdmobRewardedService(cfg.rewarded.adUnitId);

            case AdNetworkType.AppLovinMax:
                 return new MaxRewardedService(cfg.rewarded.adUnitId);

            case AdNetworkType.None:
            default:
                return null;
        }
    }

    private IBannerAdService CreateBannerService(AdsProfile cfg)
    {
        switch (cfg.banner.network)
        {
            case AdNetworkType.AdMob:
                return new AdmobBannerService(cfg.banner.adUnitId);

            case AdNetworkType.AppLovinMax:
                return new MaxBannerService(cfg.banner.adUnitId);

            case AdNetworkType.None:
            default:
                return null;
        }
    }

    #endregion

    #region Public API (게임 코드에서 사용)

    public void ShowBanner()
    {
        if (_state != AdsServiceState.Configured) return;
        _banner?.Show();
    }

    public void HideBanner()
    {
        if (_state != AdsServiceState.Configured) return;
        _banner?.Hide();
    }

    public void ShowInterstitial()
    {
        if (_state != AdsServiceState.Configured) return;
        if (_interstitial == null) return;

        if (!_interstitial.IsReady)
            _interstitial.Load();

        _interstitial.Show();
    }

    public void ShowRewarded(Action onReward)
    {
        if (_state != AdsServiceState.Configured) return;
        if (_rewarded == null) return;

        if (!_rewarded.IsReady)
            _rewarded.Load();

        _rewarded.Show(onReward);
    }

    #endregion
}
