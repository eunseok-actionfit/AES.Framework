using System;
using GoogleMobileAds.Api;
using UnityEngine;

public class AdmobBannerService : IBannerAdService, IDisposable
{
    private readonly string _adUnitId;
    private BannerView _bannerView;
    private bool _initialized;

    public AdmobBannerService(string adUnitId)
    {
        _adUnitId = adUnitId;
    }

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        // Mobile Ads SDK 초기화 (앱 실행 시 한 번만)
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // 필요하면 initStatus 사용
            Debug.Log("[AdmobBanner] MobileAds initialized.");
        });
    }

    public void LoadAndShow()
    {
        if (string.IsNullOrEmpty(_adUnitId))
        {
            Debug.LogWarning("[AdmobBanner] Ad Unit Id is empty.");
            return;
        }

        if (!_initialized)
        {
            Initialize();
        }

        if (_bannerView == null)
        {
            // 화면 하단 배너
            _bannerView = new BannerView(_adUnitId, AdSize.Banner, AdPosition.Bottom);
            RegisterBannerEvents(_bannerView);
        }

        // 공식 문서: bannerView.LoadAd(new AdRequest());
        var request = new AdRequest();
        _bannerView.LoadAd(request);

        // 로드 후 즉시 노출
        _bannerView.Show();
    }

    private void RegisterBannerEvents(BannerView bannerView)
    {
        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("[AdmobBanner] Banner loaded.");
        };

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogWarning($"[AdmobBanner] Banner failed to load: {error}");
        };

        bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(
                $"[AdmobBanner] Paid. Value: {adValue.Value}, Currency: {adValue.CurrencyCode}, Precision: {adValue.Precision}");
        };

        bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AdmobBanner] Impression recorded.");
        };

        bannerView.OnAdClicked += () =>
        {
            Debug.Log("[AdmobBanner] Banner clicked.");
        };

        bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdmobBanner] Full screen content opened.");
        };

        bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdmobBanner] Full screen content closed.");
        };
    }

    public void Hide()
    {
        _bannerView?.Hide();
    }

    public void Show()
    {
        _bannerView?.Show();
    }

    public void Dispose()
    {
        if (_bannerView != null)
        {
            _bannerView.Destroy();
            _bannerView = null;
        }
    }
}
