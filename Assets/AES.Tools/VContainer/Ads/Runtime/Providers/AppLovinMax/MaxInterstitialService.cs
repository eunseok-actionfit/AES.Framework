using System;
using System.Threading;
using AES.Tools;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.VContainer
{
    public class MaxInterstitialService : IInterstitialAdService, IDisposable
    {
        private readonly string _unitId;
        private readonly ITimerScheduler _scheduler;

        private bool _initialized;
        private bool _callbacksRegistered;
        private int _retryAttempt;

        // RunAfter는 UniTask를 반환하므로, 취소용 핸들은 CTS로 관리
        private CancellationTokenSource _retryCts;

        public MaxInterstitialService(string unitId, ITimerScheduler scheduler)
        {
            _unitId    = unitId;
            _scheduler = scheduler;
        }

        public bool IsReady =>
            !string.IsNullOrEmpty(_unitId) &&
            MaxSdk.IsInterstitialReady(_unitId);

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            RegisterCallbacks();
            Load();
        }

        public void Load()
        {
            if (string.IsNullOrEmpty(_unitId)) return;

            Debug.Log($"[MaxInterstitial] Load: {_unitId}");
            MaxSdk.LoadInterstitial(_unitId);
        }

        public bool Show()
        {
            if (!IsReady)
            {
                Debug.Log("[MaxInterstitial] Not ready.");
                return false;
            }

            Debug.Log($"[MaxInterstitial] Show: {_unitId}");
            MaxSdk.ShowInterstitial(_unitId);
            return true;
        }

        private void RegisterCallbacks()
        {
            if (_callbacksRegistered) return;
            _callbacksRegistered = true;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent        += OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent    += OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent     += OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent       += OnInterstitialClicked;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent        += OnInterstitialHidden;
        }

        private void UnregisterCallbacks()
        {
            if (!_callbacksRegistered) return;
            _callbacksRegistered = false;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent        -= OnInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent    -= OnInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent     -= OnInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent       -= OnInterstitialClicked;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent        -= OnInterstitialHidden;
        }

        // --- MAX Interstitial Callbacks ---

        private void OnInterstitialLoaded(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.Log($"[MaxInterstitial] Loaded: {adUnitId}");
            _retryAttempt = 0;
        }

        private void OnInterstitialLoadFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.LogWarning($"[MaxInterstitial] Load failed: {adUnitId}, error: {errorInfo.Message}");

            _retryAttempt++;
            double retryDelay = Math.Pow(2, Math.Min(6, _retryAttempt)); // 최대 64초

            // 이전 예약 취소
            _retryCts?.Cancel();
            _retryCts?.Dispose();

            _retryCts = new CancellationTokenSource();

            _scheduler.RunAfter(
                    TimeSpan.FromSeconds(retryDelay),
                    async ct =>
                    {
                        if (ct.IsCancellationRequested) return;
                        Load();
                        await UniTask.CompletedTask;
                    },
                    _retryCts.Token
                )
                .Forget();

            Debug.Log($"[MaxInterstitial] Retry in {retryDelay} seconds.");
        }

        private void OnInterstitialDisplayed(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;
            Debug.Log($"[MaxInterstitial] Displayed: {adUnitId}");

            // AdMob 인터스티셜과 동일하게 상태 이벤트 쏘기
            EventBus<AdShowingStateChangedEvent>.Raise(
                new AdShowingStateChangedEvent(true, AdPlacementType.Interstitial));
        }

        private void OnInterstitialDisplayFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.LogWarning($"[MaxInterstitial] Display failed: {adUnitId}, error: {errorInfo.Message}");

            // 실패 시 노출 상태 false + 인터스티셜 종료 false
            EventBus<AdShowingStateChangedEvent>.Raise(
                new AdShowingStateChangedEvent(false, AdPlacementType.Interstitial));
            EventBus<InterstitialFinishedEvent>.Raise(
                new InterstitialFinishedEvent(false));

            Load(); // 표시 실패 시 바로 재로드
        }

        private void OnInterstitialHidden(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.Log($"[MaxInterstitial] Hidden: {adUnitId}. Pre-loading next ad.");

            // 닫히면 노출 상태 false + 인터스티셜 종료 true
            EventBus<AdShowingStateChangedEvent>.Raise(
                new AdShowingStateChangedEvent(false, AdPlacementType.Interstitial));
            EventBus<InterstitialFinishedEvent>.Raise(
                new InterstitialFinishedEvent(true));

            Load(); // 다음 광고 미리 로드
        }
        
        private void OnInterstitialClicked(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;
            Debug.Log($"[MaxInterstitial] Clicked: {adUnitId}");
        }
        
        public void Dispose()
        {
            UnregisterCallbacks();

            _retryCts?.Cancel();
            _retryCts?.Dispose();
            _retryCts = null;
        }
    }
}
