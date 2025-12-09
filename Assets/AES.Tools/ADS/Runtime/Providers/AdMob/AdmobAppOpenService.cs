using System;
using GoogleMobileAds.Api;
using UnityEngine;

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
    public bool IsReady =>
        _appOpenAd != null &&
        _appOpenAd.CanShowAd() &&
        DateTime.Now < _expireTime;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        MobileAds.Initialize(initStatus =>
        {
            // 필요하면 초기화 완료 후 콜백 처리
        });
    }

    /// <summary>
    /// 앱오픈 광고 로드
    /// </summary>
    public void Load()
    {
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

        // 공식 문서: var adRequest = new AdRequest();
        var adRequest = new AdRequest();

        AppOpenAd.Load(
            _adUnitId,
            adRequest,
            (AppOpenAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("[AdmobAppOpen] Failed to load app open ad: " + error);
                    return;
                }

                Debug.Log("[AdmobAppOpen] App open ad loaded. " + ad.GetResponseInfo());

                _appOpenAd = ad;

                // 4시간 유효 시간 설정
                _expireTime = DateTime.Now + TimeSpan.FromHours(4);

                RegisterEventHandlers(ad);
            });
    }

    /// <summary>
    /// 문서 스타일의 이벤트 등록
    /// </summary>
    private void RegisterEventHandlers(AppOpenAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            // 수익 트래킹 필요하면 여기서 처리
             Debug.Log($"[AdmobAppOpen] Paid {adValue.Value} {adValue.CurrencyCode}");
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

    /// <summary>
    /// 준비돼 있으면 노출, 아니면 로드
    /// </summary>
    public void ShowIfReady()
    {
        if (IsReady)
        {
            Debug.Log("[AdmobAppOpen] Showing app open ad.");
            _appOpenAd.Show();
        }
        else
        {
            Debug.Log("[AdmobAppOpen] Ad not ready. Loading...");
            Load();
        }
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
