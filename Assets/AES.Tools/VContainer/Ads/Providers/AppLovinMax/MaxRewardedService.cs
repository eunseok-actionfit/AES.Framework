using System;
using System.Threading;
using AES.Tools.TimeManager.Schedulers;
using Cysharp.Threading.Tasks;


namespace AES.Tools.VContainer
{
    public class MaxRewardedService : IRewardedAdService, IDisposable
    {
        private readonly string _unitId;
        private readonly ITimerScheduler _scheduler;

        private int _retryAttempt;

        // RunAfter 취소용
        private CancellationTokenSource _retryCts;

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

        public MaxRewardedService(string unitId, ITimerScheduler scheduler)
        {
            _unitId    = unitId;
            _scheduler = scheduler;
        }

        public bool IsReady =>
            !string.IsNullOrEmpty(_unitId) &&
            MaxSdk.IsRewardedAdReady(_unitId);

        public void Initialize()
        {
            if (_callbacksRegistered)
                return;

            RegisterCallbacks();
            _callbacksRegistered = true;

            Load();
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
            UnregisterCallbacks();

            _onLoadedHandler = (adUnitId, adInfo) =>
            {
                if (adUnitId != _unitId) return;
                _retryAttempt = 0;
            };

            _onLoadFailedHandler = (adUnitId, errorInfo) =>
            {
                if (adUnitId != _unitId) return;

                _retryAttempt++;
                double retryDelay = Math.Pow(2, Math.Min(6, _retryAttempt));

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
            };

            _onDisplayedHandler = (adUnitId, adInfo) =>
            {
                if (adUnitId != _unitId) return;

                // 보상형 광고 표시 시작
                EventBus<AdShowingStateChangedEvent>.Raise(
                    new AdShowingStateChangedEvent(true, AdPlacementType.Rewarded));
            };

            _onDisplayFailedHandler = (adUnitId, errorInfo, adInfo) =>
            {
                if (adUnitId != _unitId) return;

                // 표시 실패 → AdMob과 동일하게 노출 종료 + 실패 이벤트
                EventBus<AdShowingStateChangedEvent>.Raise(
                    new AdShowingStateChangedEvent(false, AdPlacementType.Rewarded));
                EventBus<RewardedFinishedEvent>.Raise(
                    new RewardedFinishedEvent(false, false));

                Load();
            };

            _onHiddenHandler = (adUnitId, adInfo) =>
            {
                if (adUnitId != _unitId) return;

                // 광고 닫힘 → 노출 상태 false
                EventBus<AdShowingStateChangedEvent>.Raise(
                    new AdShowingStateChangedEvent(false, AdPlacementType.Rewarded));

                // AdMob처럼 광고 종료 이벤트(보상 여부는 true/false2번째 인자로 구분)
                EventBus<RewardedFinishedEvent>.Raise(
                    new RewardedFinishedEvent(true, false));

                // 다음 광고 미리 로드
                Load();

                _pendingRewardAction = null;
            };

            _onReceivedRewardHandler = (adUnitId, reward, adInfo) =>
            {
                if (adUnitId != _unitId) return;

                // 실제 보상 지급
                _pendingRewardAction?.Invoke();
                _pendingRewardAction = null;

                // 보상 획득 이벤트 (AdMob의 Show 내 콜백과 동일한 의미)
                EventBus<RewardedFinishedEvent>.Raise(
                    new RewardedFinishedEvent(true, true));
            };

            _onClickedHandler = (adUnitId, adInfo) =>
            {
                if (adUnitId != _unitId) return;
            };

            _onRevenuePaidHandler = (adUnitId, adInfo) =>
            {
                if (adUnitId != _unitId) return;
            };

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent         += _onLoadedHandler;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent     += _onLoadFailedHandler;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent      += _onDisplayedHandler;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent  += _onDisplayFailedHandler;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent         += _onHiddenHandler;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += _onReceivedRewardHandler;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent        += _onClickedHandler;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent    += _onRevenuePaidHandler;
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

            _retryCts?.Cancel();
            _retryCts?.Dispose();
            _retryCts = null;
        }
    }
}
