#if AESFW_ADS_ADMOB && AESFW_UNITASK
using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using Singular;
using UnityEngine;


namespace AES.Tools.VContainer
{
    public class AdmobAppOpenService : IAppOpenAdService, IDisposable
    {
        private readonly string _adUnitId;

        private AppOpenAd _appOpenAd;
        private bool _initialized;
        private DateTime _expireTime;

        public AdmobAppOpenService(string adUnitId)
        {
            _adUnitId = adUnitId;
        }

        // 문서 기준: 로드 후 4시간까지 유효, 그리고 CanShowAd() 확인
        public bool IsReady
        {
            get
            {
                bool ready = _appOpenAd != null &&
                             _appOpenAd.CanShowAd() &&
                             DateTime.Now < _expireTime;

                Debug.Log($"[AdmobAppOpen] IsReady = {ready}, " +
                          $"hasAd={_appOpenAd != null}, " +
                          $"canShow={_appOpenAd?.CanShowAd()}, " +
                          $"expire={_expireTime}, now={DateTime.Now}");

                return ready;
            }
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            MobileAds.Initialize(initStatus =>
            {
                Debug.Log("[AdmobAppOpen] Initialized");
                Load(); // 여기서 로드해야 함
            });
        }

        /// <summary>
        /// 앱오픈 광고 로드
        /// </summary>
        public void Load()
        {
            Debug.Log("[AppOpen] Load requested");
            
            if (string.IsNullOrEmpty(_adUnitId))
            {
                Debug.LogWarning("[AdmobAppOpen] Ad unit id is empty.");
                return;
            }

            // 기존 광고 정리
            if (_appOpenAd != null)
            {
                _appOpenAd.Destroy();
                _appOpenAd = null;
            }

            Debug.Log("[AdmobAppOpen] Loading app open ad.");

            var adRequest = new AdRequest();

            AppOpenAd.Load(
                _adUnitId,
                adRequest,
                (AppOpenAd ad, LoadAdError error) =>
                {
                    if (error != null || ad == null)
                    {
                        Debug.LogError("[AdmobAppOpen] Failed to load. Retrying in 5s");
                        _ = UniTask.Delay(5000).ContinueWith(Load);
                        return;
                    }

                    Debug.Log("[AdmobAppOpen] App open ad loaded. " + ad.GetResponseInfo());

                    _appOpenAd = ad;

                    // 4시간 유효 시간 설정
                    _expireTime = DateTime.Now + TimeSpan.FromHours(4);

                    RegisterEventHandlers(ad);
                });
        }

        private void RegisterEventHandlers(AppOpenAd ad)
        {
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log($"[AdmobAppOpen] Paid {adValue.Value} {adValue.CurrencyCode}");
                // Validate and ensure revenue data is within an expected range
                float revenue = adValue.Value / 1_000_000f; // Convert micros to dollars
                string currency = adValue.CurrencyCode;

                // Check if revenue is positive and currency is valid
                if (revenue > 0 && !string.IsNullOrEmpty(currency))
                {
                    // Construct and send the Singular AdMon Event
                    SingularAdData data = new SingularAdData(
                        "Admob",
                        currency,
                        revenue
                    );
                    SingularSDK.AdRevenue(data);

                    // Log the revenue data for debugging purposes
                    Debug.Log($"Ad Revenue reported to Singular: {data}");
                }
                else
                {
                    Debug.LogError($"Invalid ad revenue data: revenue = {revenue}, currency = {currency}");
                }
            };

            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[AdmobAppOpen] Impression recorded.");
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("[AdmobAppOpen] Clicked.");
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[AdmobAppOpen] Full screen content opened.");
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdmobAppOpen] Full screen content closed. Preloading next.");
                // 광고 한 번 쓰고 나면 새로 로드
                Load();
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("[AdmobAppOpen] Failed to open full screen content: " + error);
            };
        }
        
        public void ShowIfReady()
        {
            if (!IsReady)
            {
                Debug.Log("[AdmobAppOpen] ShowIfReady called but not ready.");
                return;
            }

            Debug.Log("[AdmobAppOpen] Showing app open ad.");
            _appOpenAd.Show();
        }

        public void Dispose()
        {
            if (_appOpenAd != null)
            {
                _appOpenAd.Destroy();
                _appOpenAd = null;
            }
        }
    }
}
#endif