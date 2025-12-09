using System;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using UnityEngine;

public class AdmobRewardedService : IRewardedAdService, IDisposable
{
    private readonly string _unitId;
    private RewardedAd _rewardedAd;
    private bool _initialized;
    private bool _isLoading;

    public AdmobRewardedService(string unitId)
    {
        _unitId = unitId;
    }

    public bool IsReady =>
        _rewardedAd != null && _rewardedAd.CanShowAd();

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        MobileAds.Initialize((InitializationStatus status) =>
        {
            Debug.Log("[AdmobRewarded] MobileAds initialized.");
        });
    }

    public void Load()
    {
        if (_isLoading || string.IsNullOrEmpty(_unitId))
            return;

        if (!_initialized)
        {
            Initialize();
        }

        _isLoading = true;
        Debug.Log("[AdmobRewarded] Loading rewarded...");

        // 공식 문서: var adRequest = new AdRequest();
        var request = new AdRequest();

        RewardedAd.Load(
            _unitId,
            request,
            (ad, error) =>
            {
                _isLoading = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning($"[AdmobRewarded] Load failed: {error}");
                    return;
                }

                Debug.Log("[AdmobRewarded] Rewarded loaded.");
                _rewardedAd = ad;

                RegisterEvents(ad);
            });
    }

    public bool Show(Action onReward)
    {
        if (!IsReady)
        {
            Debug.Log("[AdmobRewarded] Not ready.");
            return false;
        }

        _rewardedAd.Show((Reward reward) =>
        {
            Debug.Log($"[AdmobRewarded] Reward earned. Type:{reward.Type}, Amount:{reward.Amount}");
            onReward?.Invoke();
        });

        return true;
    }

    private void RegisterEvents(RewardedAd ad)
    {
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"[AdmobRewarded] Paid. Value:{adValue.Value}, Currency:{adValue.CurrencyCode}, Precision:{adValue.Precision}");
        };

        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("[AdmobRewarded] Impression recorded.");
        };

        ad.OnAdClicked += () =>
        {
            Debug.Log("[AdmobRewarded] Ad clicked.");
        };

        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[AdmobRewarded] Full screen opened.");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdmobRewarded] Full screen closed.");
            // RewardedAd는 1회용 → 정리 후 다시 로드
            CleanupAd();
            Load();
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogWarning($"[AdmobRewarded] Full screen failed: {error}");
            CleanupAd();
        };
    }

    private void CleanupAd()
    {
        if (_rewardedAd == null)
            return;

        _rewardedAd.Destroy();
        _rewardedAd = null;
    }

    public void Dispose()
    {
        CleanupAd();
        _isLoading = false;
    }
}
