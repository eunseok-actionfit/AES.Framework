#if AESFW_ADS_ADMOB && AESFW_UNITASK
using System;
using AES.Tools.UI;
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
                // 필요하면 Adaptive로 교체 가능:
                // var adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                // _bannerView = new BannerView(_adUnitId, adaptiveSize, AdPosition.Bottom);

                _bannerView = new BannerView(_adUnitId, AdSize.Banner, AdPosition.Bottom);
                RegisterBannerEvents(_bannerView);
            }

            var request = new AdRequest();
            _bannerView.LoadAd(request);

            // HeightPx는 로드 콜백에서 올리는 게 정석
            _bannerView.Show();
        }

        public void Hide()
        {
            if (_bannerView != null)
            {
                _bannerView.Hide();
                EventBus<BannerHeightChangedEvent>.Raise(new BannerHeightChangedEvent(0, false));
            }
        }

        private void RegisterBannerEvents(BannerView bannerView)
        {
            bannerView.OnBannerAdLoaded += () =>
            {
                Debug.Log("[AdmobBanner] Banner loaded.");

                int heightPx = GetBannerHeightPx(bannerView);
                EventBus<BannerHeightChangedEvent>.Raise(new BannerHeightChangedEvent(heightPx, true));
            };

            bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                Debug.LogWarning($"[AdmobBanner] Banner failed to load: {error}");
                EventBus<BannerHeightChangedEvent>.Raise(new BannerHeightChangedEvent(0, false));
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

        /// <summary>
        /// AdMob AdSize는 dp 단위. dp -> px 변환해서 UI에 전달.
        /// </summary>
        private int GetBannerHeightPx(BannerView bannerView)
        {
            // 스샷 기준: BannerView에 이 API는 존재함
            float h = bannerView.GetHeightInPixels();
            int heightPx = Mathf.RoundToInt(h);

            // 가끔 0이 올 수 있으니 최소값 방어(선택)
            return Mathf.Max(0, heightPx);
        }

        public void Dispose()
        {
            if (_bannerView != null)
            {
                _bannerView.Destroy();
                _bannerView = null;
            }

            EventBus<BannerHeightChangedEvent>.Raise(new BannerHeightChangedEvent(0, false));
        }
    }
}
#endif
