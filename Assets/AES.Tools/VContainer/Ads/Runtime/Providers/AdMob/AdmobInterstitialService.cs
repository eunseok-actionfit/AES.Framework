using System;
using AES.Tools;
using GoogleMobileAds.Api;
using UnityEngine;


namespace AES.Tools.VContainer
{
    public class AdmobInterstitialService : IInterstitialAdService, IDisposable
    {
        private readonly string _unitId;

        private InterstitialAd _interstitialAd;
        private bool _initialized;
        private bool _isLoading;

        public AdmobInterstitialService(string unitId)
        {
            _unitId = unitId;
        }

        public bool IsReady =>
            _interstitialAd != null && _interstitialAd.CanShowAd();

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            MobileAds.Initialize((InitializationStatus status) =>
            {
                Debug.Log("[AdmobInterstitial] MobileAds initialized.");
            });
        }

        public void Load()
        {
            if (_isLoading || string.IsNullOrEmpty(_unitId))
                return;

            _isLoading = true;

            Debug.Log("[AdmobInterstitial] Loading interstitial...");

            var request = new AdRequest();

            InterstitialAd.Load(_unitId, request, (ad, error) =>
            {
                _isLoading = false;

                if (error != null)
                {
                    Debug.LogWarning($"[AdmobInterstitial] Load failed: {error}");
                    return;
                }

                _interstitialAd = ad;

                RegisterEvents(ad);

                Debug.Log("[AdmobInterstitial] Interstitial loaded.");
            });
        }

        public bool Show()
        {
            if (!IsReady)
            {
                Debug.Log("[AdmobInterstitial] Not ready.");
                return false;
            }

            _interstitialAd.Show();
            return true;
        }

        private void RegisterEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[AdmobInterstitial] Full screen content opened.");
                EventBus<AdShowingStateChangedEvent>.Raise(
                    new AdShowingStateChangedEvent(true, AdPlacementType.Interstitial));
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdmobInterstitial] Full screen content closed.");
                EventBus<AdShowingStateChangedEvent>.Raise(
                    new AdShowingStateChangedEvent(false, AdPlacementType.Interstitial));
                EventBus<InterstitialFinishedEvent>.Raise(new InterstitialFinishedEvent(true));

                CleanupAd();
                Load(); // 인터스티셜은 1회성 → 다시 로드
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogWarning($"[AdmobInterstitial] Full screen failed: {error}");
                EventBus<AdShowingStateChangedEvent>.Raise(
                    new AdShowingStateChangedEvent(false, AdPlacementType.Interstitial));
                EventBus<InterstitialFinishedEvent>.Raise(new InterstitialFinishedEvent(false));

                CleanupAd();
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("[AdmobInterstitial] Ad clicked.");
            };

            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[AdmobInterstitial] Impression recorded.");
            };

            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log(
                    $"[AdmobInterstitial] Paid: {adValue.Value} {adValue.CurrencyCode}, Precision: {adValue.Precision}"
                );
            };
        }

        private void CleanupAd()
        {
            if (_interstitialAd == null)
                return;

            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        public void Dispose()
        {
            CleanupAd();
            _isLoading = false;
        }
    }
}
