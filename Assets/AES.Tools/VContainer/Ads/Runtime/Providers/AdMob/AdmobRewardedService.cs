using System;
using AES.Tools;
using GoogleMobileAds.Api;
using UnityEngine;


namespace AES.Tools.VContainer
{
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

                EventBus<RewardedFinishedEvent>.Raise(
                    new RewardedFinishedEvent(true, true));
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
                EventBus<AdShowingStateChangedEvent>.Raise(
                    new AdShowingStateChangedEvent(true, AdPlacementType.Rewarded));
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[AdmobRewarded] Full screen closed.");
                EventBus<AdShowingStateChangedEvent>.Raise(
                    new AdShowingStateChangedEvent(false, AdPlacementType.Rewarded));

                // RewardedAd는 1회용 → 정리 후 다시 로드
                CleanupAd();
                Load();

                // 광고는 끝났지만 보상을 받았는지는 Show()에서 처리 완료
                EventBus<RewardedFinishedEvent>.Raise(new RewardedFinishedEvent(true, false));
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogWarning($"[AdmobRewarded] Full screen failed: {error}");
                EventBus<AdShowingStateChangedEvent>.Raise(
                    new AdShowingStateChangedEvent(false, AdPlacementType.Rewarded));
                EventBus<RewardedFinishedEvent>.Raise(new RewardedFinishedEvent(false, false));
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
}
