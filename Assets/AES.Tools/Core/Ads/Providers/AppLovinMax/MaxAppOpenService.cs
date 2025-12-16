#if AESFW_ADS_MAX
using System;
using UnityEngine;


// AppOpen 전용 서비스
namespace AES.Tools.VContainer
{
    public class MaxAppOpenService : IAppOpenAdService, IDisposable
    {
        private readonly string _unitId;

        private bool _initialized;
        private bool _callbacksRegistered;

        public MaxAppOpenService(string unitId)
        {
            _unitId = unitId;
        }

        public bool IsReady
        {
            get
            {
                bool ready = MaxSdk.IsAppOpenAdReady(_unitId);
                Debug.Log($"[MaxAppOpen] IsReady = {ready}");
                return ready;
            }
        }


        public void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            

            RegisterCallbacks();
        }

        public void Load()
        {
            Debug.Log("[AppOpen] Load requested");
            if (string.IsNullOrEmpty(_unitId))
                return;

            // SDK 초기화가 안 되어 있으면 로드 시 에러가 날 수 있으므로 가드
            if (!_initialized)
            {
                Debug.LogWarning("[MaxAppOpen] Load() called before Initialize().");
            }

            Debug.Log($"[MaxAppOpen] Load app open ad: {_unitId}");
            MaxSdk.LoadAppOpenAd(_unitId);
        }

    

        public void ShowIfReady()
        {
            if (!IsReady)
            {
                Debug.Log("[MaxAppOpen] ShowIfReady called but not ready.");
                return;
            }

            Debug.Log($"[MaxAppOpen] Show app open ad: {_unitId}");
            MaxSdk.ShowAppOpenAd(_unitId);
        }

        private void RegisterCallbacks()
        {
            if (_callbacksRegistered)
                return;

            _callbacksRegistered = true;

            // AppOpen 콜백 등록
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAdLoaded;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAdLoadFailed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAdDisplayed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAdDisplayFailed;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAdClicked;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAdHidden;
        }

        private void UnregisterCallbacks()
        {
            if (!_callbacksRegistered)
                return;

            _callbacksRegistered = false;

            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnAdLoaded;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnAdLoadFailed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnAdDisplayed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnAdDisplayFailed;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent -= OnAdClicked;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnAdHidden;
        }

        #region MAX Callbacks

        private void OnAdLoaded(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.Log($"[MaxAppOpen] Ad loaded: {adUnitId}, network: {adInfo.NetworkName}");
        }

        private void OnAdLoadFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.LogWarning($"[MaxAppOpen] Load failed: {adUnitId}, error: {errorInfo.Message}");
            // 필요시 재시도 로직 / 쿨다운 등 여기에서 처리
        }

        private void OnAdDisplayed(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.Log($"[MaxAppOpen] Ad displayed: {adUnitId}");
        }

        private void OnAdDisplayFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.LogWarning($"[MaxAppOpen] Display failed: {adUnitId}, error: {errorInfo.Message}");
            // 실패 시 다음 기회 대비 로드
            Load();
        }

        private void OnAdClicked(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.Log($"[MaxAppOpen] Ad clicked: {adUnitId}");
        }

        // 공식 문서 예제: OnAppOpenDismissedEvent에서 다시 Load 호출
        private void OnAdHidden(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.Log($"[MaxAppOpen] Ad hidden: {adUnitId}. Reloading...");
            Load();
        }

        #endregion

        public void Dispose()
        {
            UnregisterCallbacks();
            // App open 광고는 Destroy API 없음. 새 로드는 MaxSdk.LoadAppOpenAd()로만.
        }
    }
}
#endif