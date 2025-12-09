using System;

public class MaxRewardedService : IRewardedAdService, IDisposable
{
    private readonly string _unitId;

    // 공식 샘플처럼 재시도 카운터(필요 없으면 제거해도 됨)
    private int _retryAttempt;

    // 현재 노출에 대한 보상 콜백
    private Action _pendingRewardAction;

    // 이벤트 핸들러 캐시 (구독/해제용)
    private Action<string, MaxSdkBase.AdInfo> _onLoadedHandler;
    private Action<string, MaxSdkBase.ErrorInfo> _onLoadFailedHandler;
    private Action<string, MaxSdkBase.AdInfo> _onDisplayedHandler;
    private Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> _onDisplayFailedHandler;
    private Action<string, MaxSdkBase.AdInfo> _onHiddenHandler;
    private Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> _onReceivedRewardHandler;
    private Action<string, MaxSdkBase.AdInfo> _onClickedHandler;
    private Action<string, MaxSdkBase.AdInfo> _onRevenuePaidHandler;

    private bool _callbacksRegistered;

    public MaxRewardedService(string unitId)
    {
        _unitId = unitId;
    }

    public bool IsReady =>
        !string.IsNullOrEmpty(_unitId) &&
        MaxSdk.IsRewardedAdReady(_unitId);

    public void Initialize()
    {
        // MaxSdk.Initialize("APPLOVIN_SDK_KEY"); // 앱 전체에서 1회

        if (_callbacksRegistered)
            return;

        RegisterCallbacks();
        _callbacksRegistered = true;
    }

    public void Load()
    {
        if (string.IsNullOrEmpty(_unitId))
            return;

        MaxSdk.LoadRewardedAd(_unitId);
    }

    public bool Show(Action onReward)
    {
        if (!IsReady)
            return false;

        _pendingRewardAction = onReward;
        MaxSdk.ShowRewardedAd(_unitId);
        return true;
    }

    private void RegisterCallbacks()
    {
        // 한 번이라도 등록됐을 수 있으니 먼저 모두 해제 후 재등록
        UnregisterCallbacks();

        _onLoadedHandler = (adUnitId, adInfo) =>
        {
            if (adUnitId != _unitId) return;
            _retryAttempt = 0;
            // 필요하면 로그/트래킹
            // Debug.Log($"[MaxRewarded] Loaded: {adUnitId}");
        };

        _onLoadFailedHandler = (adUnitId, errorInfo) =>
        {
            if (adUnitId != _unitId) return;
            _retryAttempt++;
            double retryDelay = Math.Pow(2, Math.Min(6, _retryAttempt));
            // 필요하면 코루틴으로 retryDelay 후 Load() 호출
            // Debug.Log($"[MaxRewarded] Load failed: {errorInfo.Message}, retry in {retryDelay}s");
        };

        _onDisplayedHandler = (adUnitId, adInfo) =>
        {
            if (adUnitId != _unitId) return;
            // Debug.Log($"[MaxRewarded] Displayed: {adUnitId}");
        };

        _onDisplayFailedHandler = (adUnitId, errorInfo, adInfo) =>
        {
            if (adUnitId != _unitId) return;
            // Debug.LogError($"[MaxRewarded] Display failed: {errorInfo.Message}");
            Load();
        };

        _onHiddenHandler = (adUnitId, adInfo) =>
        {
            if (adUnitId != _unitId) return;
            // 광고 닫힌 뒤 다음 광고 미리 로드
            Load();
            // 이번 노출에 대한 pendingRewardAction은 OnAdReceivedRewardEvent에서 이미 호출/정리됨
            _pendingRewardAction = null;
        };

        _onReceivedRewardHandler = (adUnitId, reward, adInfo) =>
        {
            if (adUnitId != _unitId) return;

            // 보상 지급
            _pendingRewardAction?.Invoke();
            _pendingRewardAction = null;
        };

        _onClickedHandler = (adUnitId, adInfo) =>
        {
            if (adUnitId != _unitId) return;
            // Debug.Log($"[MaxRewarded] Clicked: {adUnitId}");
        };

        _onRevenuePaidHandler = (adUnitId, adInfo) =>
        {
            if (adUnitId != _unitId) return;
            // eCPM 추적 등 필요 시 사용
        };

        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent          += _onLoadedHandler;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent      += _onLoadFailedHandler;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent       += _onDisplayedHandler;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent   += _onDisplayFailedHandler;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent          += _onHiddenHandler;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent  += _onReceivedRewardHandler;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent         += _onClickedHandler;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent     += _onRevenuePaidHandler;
    }

    private void UnregisterCallbacks()
    {
        if (_onLoadedHandler != null)
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= _onLoadedHandler;
        if (_onLoadFailedHandler != null)
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= _onLoadFailedHandler;
        if (_onDisplayedHandler != null)
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= _onDisplayedHandler;
        if (_onDisplayFailedHandler != null)
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= _onDisplayFailedHandler;
        if (_onHiddenHandler != null)
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= _onHiddenHandler;
        if (_onReceivedRewardHandler != null)
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= _onReceivedRewardHandler;
        if (_onClickedHandler != null)
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= _onClickedHandler;
        if (_onRevenuePaidHandler != null)
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= _onRevenuePaidHandler;

        _onLoadedHandler         = null;
        _onLoadFailedHandler     = null;
        _onDisplayedHandler      = null;
        _onDisplayFailedHandler  = null;
        _onHiddenHandler         = null;
        _onReceivedRewardHandler = null;
        _onClickedHandler        = null;
        _onRevenuePaidHandler    = null;
    }

    public void Dispose()
    {
        UnregisterCallbacks();
        _pendingRewardAction = null;
    }
}
