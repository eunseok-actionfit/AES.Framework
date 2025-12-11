using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace AES.Tools.VContainer
{
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

            MobileAds.Initialize((InitializationStatus initStatus) =>
            {
                Debug.Log("[AdmobBanner] MobileAds initialized.");
            });
        }

        public void Show()
        {
            if (string.IsNullOrEmpty(_adUnitId))
            {
                Debug.LogWarning("[AdmobBanner] Ad Unit Id is empty.");
                return;
            }

            if (!_initialized)
                Initialize();

            if (_bannerView == null)
            {
                _bannerView = new BannerView(_adUnitId, AdSize.Banner, AdPosition.Bottom);
                RegisterBannerEvents(_bannerView);
            }

            var request = new AdRequest();
            _bannerView.LoadAd(request);

            _bannerView.Show();
        }

        public void Hide()
        {
            _bannerView?.Hide();
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

        public void Dispose()
        {
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
            }
        }
    }
}
