using System;
using UnityEngine;

public class MaxInterstitialService : IInterstitialAdService, IDisposable
{
    private readonly string _unitId;
    private bool _initialized;
    private bool _callbacksRegistered;
    private int _retryAttempt;

    public MaxInterstitialService(string unitId)
    {
        _unitId = unitId;
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
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClicked;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
    }

    private void UnregisterCallbacks()
    {
        if (!_callbacksRegistered) return;
        _callbacksRegistered = false;
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialDisplayFailed;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialClicked;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHidden;
    }

    // --- MAX Interstitial Callbacks ---
    private void OnInterstitialLoaded(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        if (adUnitId != _unitId) return;
        Debug.Log($"[MaxInterstitial] Loaded: {adUnitId}");
        _retryAttempt = 0; // 성공시 시도횟수 초기화
    }

    private void OnInterstitialLoadFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo)
    {
        if (adUnitId != _unitId) return;
        Debug.LogWarning($"[MaxInterstitial] Load failed: {adUnitId}, error: {errorInfo.Message}");
        _retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, _retryAttempt)); // 최대 64초
        // MonoBehaviour 환경에서: Invoke("Load", (float)retryDelay);
        // 그렇지 않으면, 외부에서 타이머 등으로 delay 후 Load() 호출
        Debug.Log($"[MaxInterstitial] Retry in {retryDelay} seconds (implement timer/coroutine in caller). ");
    }

    private void OnInterstitialDisplayed(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        if (adUnitId != _unitId) return;
        Debug.Log($"[MaxInterstitial] Displayed: {adUnitId}");
    }

    private void OnInterstitialDisplayFailed(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
    {
        if (adUnitId != _unitId) return;
        Debug.LogWarning($"[MaxInterstitial] Display failed: {adUnitId}, error: {errorInfo.Message}");
        Load(); // 표시 실패시 즉시 다음 로드
    }

    private void OnInterstitialClicked(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        if (adUnitId != _unitId) return;
        Debug.Log($"[MaxInterstitial] Clicked: {adUnitId}");
    }

    private void OnInterstitialHidden(string adUnitId, MaxSdk.AdInfo adInfo)
    {
        if (adUnitId != _unitId) return;
        Debug.Log($"[MaxInterstitial] Hidden: {adUnitId}. Pre-loading next ad.");
        Load(); // 닫힐 때 바로 다음 광고 미리 로드
    }

    public void Dispose()
    {
        UnregisterCallbacks();
    }
}