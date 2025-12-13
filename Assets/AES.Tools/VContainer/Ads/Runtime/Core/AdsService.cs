using System;
using System.Threading;
using AES.Tools.TimeManager.Schedulers;
using AES.Tools.UI;
using AES.Tools.UI.Utility;
using AES.Tools.VContainer.AppLifetime;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.VContainer
{
    public enum AdsServiceState { Uninitialized, Configured, Disposed }

    public class AdsService : IAdsService
    {
        private readonly ITimerScheduler _scheduler;
        private readonly AdsRuntimeConfig _runtimeConfig;
        private readonly TestDeviceFlags _testDeviceFlags; // ★ 테스트폰 플래그 주입

        private AdsServiceState _state = AdsServiceState.Uninitialized;

        private AdsProfile _profile;
        private EventBinding<ApplicationFocusChangedEvent> _focusBinding;
        private EventBinding<ApplicationPauseChangedEvent> _pauseBinding;
        private EventBinding<ApplicationQuitEvent> _quitBinding;

        private IAppOpenAdService _appOpen;
        private IInterstitialAdService _interstitial;
        private IRewardedAdService _rewarded;
        private IBannerAdService _banner;

        private AdNetworkType _bannerNetwork;
        
        // 빈도 제어용 내부 캐시
        private DateTime _lastInterstitialUtc;
        private int _interstitialCountThisSession;

        private bool _runtimeDisabled;

        public bool IsReadyInterstitial => !_runtimeDisabled && (_interstitial?.IsReady ?? false);
        public bool IsReadyRewarded => !_runtimeDisabled && (_rewarded?.IsReady ?? false);
        public bool IsReadyAppOpen => _appOpen != null && _appOpen.IsReady;

        public AdsService(ITimerScheduler scheduler, AdsRuntimeConfig runtimeConfig, TestDeviceFlags testDeviceFlags)
        {
            _scheduler = scheduler;
            _runtimeConfig = runtimeConfig;
            _testDeviceFlags = testDeviceFlags;

            // 우선순위:
            // 1) CSV 기반 테스트 디바이스 adsDisabled
            // 2) RuntimeConfig 기본값
            var csvAdsDisabled = _testDeviceFlags != null && _testDeviceFlags.adsDisabled;
            var cfgAdsDisabled = _runtimeConfig != null && _runtimeConfig.runtimeAdsDisabled;

            _runtimeDisabled = csvAdsDisabled || cfgAdsDisabled;

#if UNITY_EDITOR
            if (_testDeviceFlags != null && _testDeviceFlags.isTester) { Debug.Log($"[AdsService] Test device detected: name={_testDeviceFlags.matchedName}, id={_testDeviceFlags.matchedDeviceId}, adsDisabled={_runtimeDisabled}"); }
#endif
        }

        public void SetRuntimeDisabled(bool disabled)
        {
            _runtimeDisabled = disabled;
            if (disabled)
                _banner?.Hide();
        }

        public void Configure(AdsProfile profile)
        {
            if (_state == AdsServiceState.Disposed)
            {
                Debug.LogWarning("[AdsService] Already disposed. Configure ignored.");
                return;
            }

            if (_state == AdsServiceState.Configured)
            {
                Debug.Log("[AdsService] Already configured. Skipping reconfigure.");
                return;
            }

            _profile = profile;
            _state = AdsServiceState.Configured;
            
            _bannerNetwork = _profile.banner.network;

            SetupProviders();
            SetupFocusBinding();
            SetupQuitBinding();
 #if UNITY_EDITOR
            SetupPauseBinding();
        #endif
        }

        private void SetupProviders()
        {
            _appOpen = CreateAppOpenService(_profile);
            _interstitial = CreateInterstitialService(_profile);
            _rewarded = CreateRewardedService(_profile);
            _banner = CreateBannerService(_profile);

            _appOpen?.Initialize();
            _appOpen?.Load();

            _interstitial?.Initialize();
            _interstitial?.Load();

            _rewarded?.Initialize();
            _rewarded?.Load();

            _banner?.Initialize();
        }


        private void SetupPauseBinding()
        {
            _pauseBinding = new EventBinding<ApplicationPauseChangedEvent>()
                .Add(e => {
                    var ready = IsReadyAppOpen;

                    Debug.Log(
                        $"[AdsService] Paused={e.Paused}, ready={ready}, runtimeDisabled={_runtimeDisabled}"
                    );

                    if (e.Paused) return;
                    if (_runtimeDisabled) return;

                    if (!ready)
                    {
                        Debug.Log("[AdsService] AppOpen NOT ready at focus. (skip)");
                        return;
                    }

                    Debug.Log("[AdsService] AppOpen READY at focus → Show");
                    _appOpen.ShowIfReady();
                })
                .Register();
        }


        private void SetupFocusBinding()
        {
            _focusBinding = new EventBinding<ApplicationFocusChangedEvent>()
                .Add(e => {
                    var ready = IsReadyAppOpen;

                    Debug.Log(
                        $"[AdsService] Focus={e.Focused}, ready={ready}, runtimeDisabled={_runtimeDisabled}"
                    );

                    if (!e.Focused) return;
                    if (_runtimeDisabled) return;

                    if (!ready)
                    {
                        Debug.Log("[AdsService] AppOpen NOT ready at focus. (skip)");
                        if (_appOpen != null) _appOpen.Load(); // 준비 안 된 경우 즉시 로드 재시도
                        return;
                    }

                    Debug.Log("[AdsService] AppOpen READY at focus → Show");
                    _appOpen.ShowIfReady();
                })
                .Register();
        }

        private void SetupQuitBinding()
        {
            _quitBinding = new EventBinding<ApplicationQuitEvent>()
                .Add(Dispose)
                .Register();
        }

        public void Dispose()
        {
            if (_state == AdsServiceState.Disposed)
                return;

            _state = AdsServiceState.Disposed;

            _focusBinding?.Deregister();
            _quitBinding?.Deregister();
            _focusBinding = null;
            _quitBinding = null;

            (_appOpen as IDisposable)?.Dispose();
            (_interstitial as IDisposable)?.Dispose();
            (_rewarded as IDisposable)?.Dispose();
            (_banner as IDisposable)?.Dispose();

            _appOpen = null;
            _interstitial = null;
            _rewarded = null;
            _banner = null;
        }

        // -------------------------------------------------------------------------
        // 공통: 네트워크별 서비스 생성
        // -------------------------------------------------------------------------

        private IAppOpenAdService CreateAppOpenService(AdsProfile cfg)
        {
            switch (cfg.appOpen.network)
            {
                case AdNetworkType.AdMob:
                    return new AdmobAppOpenService(cfg.appOpen.adUnitId);

                case AdNetworkType.AppLovinMax:
                    return new MaxAppOpenService(cfg.appOpen.adUnitId);

                default:
                    return null;
            }
        }

        private IInterstitialAdService CreateInterstitialService(AdsProfile cfg)
        {
            switch (cfg.interstitial.network)
            {
                case AdNetworkType.AdMob:
                    return new AdmobInterstitialService(cfg.interstitial.adUnitId);

                case AdNetworkType.AppLovinMax:
                    return new MaxInterstitialService(cfg.interstitial.adUnitId, _scheduler);

                default:
                    return null;
            }
        }

        private IRewardedAdService CreateRewardedService(AdsProfile cfg)
        {
            // 기존 버그 (cfg.interstitial.network) 이미 수정됨
            switch (cfg.rewarded.network)
            {
                case AdNetworkType.AdMob:
                    return new AdmobRewardedService(cfg.rewarded.adUnitId);

                case AdNetworkType.AppLovinMax:
                    return new MaxRewardedService(cfg.rewarded.adUnitId, _scheduler);

                default:
                    return null;
            }
        }

        private IBannerAdService CreateBannerService(AdsProfile cfg)
        {
            switch (cfg.banner.network)
            {
                case AdNetworkType.AdMob:
                    return new AdmobBannerService(cfg.banner.adUnitId);

                case AdNetworkType.AppLovinMax:
                    return new MaxBannerService(cfg.banner.adUnitId);

                default:
                    return null;
            }
        }

        // -------------------------------------------------------------------------
        // Public API — 게임 코드에서 사용
        // -------------------------------------------------------------------------

        public void ShowBanner()
        {
            if (_state != AdsServiceState.Configured) return;
            if (_runtimeDisabled) return;

            _banner?.Show();

            int bannerHeightPx = 0;

            switch (_bannerNetwork)
            {
                case AdNetworkType.AdMob:
                    // AdMob Adaptive
                    bannerHeightPx = BannerHeightUtil.GetAdmobAdaptiveBannerHeightPx();
                    break;

                case AdNetworkType.AppLovinMax:
                    // MAX Adaptive
                    bannerHeightPx = BannerHeightUtil.GetMaxAdaptiveBannerHeightPx();
                    break;

                default:
                    //  fallback
                    bannerHeightPx = BannerHeightUtil.GetDefaultBannerHeightPx();
                    break;
            }

            // SafeAreaFitter 등에 브로드캐스트
            EventBus<BannerHeightChangedEvent>.Raise(
                new BannerHeightChangedEvent(bannerHeightPx, true)
            );
        }

        public void HideBanner()
        {
            if (_state != AdsServiceState.Configured) return;

            _banner?.Hide();

            // 0, Visible=false 로 브로드캐스트
            EventBus<BannerHeightChangedEvent>.Raise(
                new BannerHeightChangedEvent(0, false)
            );
        }

        // 전면 빈도 제한
        private bool CanShowInterstitial()
        {
            if (_runtimeDisabled) return false;
            if (_interstitial == null || !_interstitial.IsReady) return false;

            var intervalSeconds = _runtimeConfig != null
                ? _runtimeConfig.interstitialMinIntervalSeconds
                : 0f;

            var interval = TimeSpan.FromSeconds(intervalSeconds);

            if (DateTime.UtcNow - _lastInterstitialUtc < interval)
                return false;

            if (_runtimeConfig != null &&
                _interstitialCountThisSession >= _runtimeConfig.interstitialMaxPerSession)
                return false;

            return true;
        }

        public void ShowInterstitial()
        {
            if (_state != AdsServiceState.Configured) return;

            if (!CanShowInterstitial())
            {
                if (!_interstitial?.IsReady ?? false)
                    _interstitial?.Load();

                return;
            }

            if (_interstitial.Show())
            {
                _lastInterstitialUtc = DateTime.UtcNow;
                _interstitialCountThisSession++;
            }
        }

        public void ShowRewarded(Action onReward)
        {
            if (_state != AdsServiceState.Configured) return;
            if (_runtimeDisabled) return;
            if (_rewarded == null) return;

            if (!_rewarded.IsReady)
                _rewarded.Load();

            _rewarded.Show(onReward);
        }


        public async UniTask<bool> ShowInterstitialAsync(CancellationToken ct = default)
        {
            if (_state != AdsServiceState.Configured) return false;
            if (_runtimeDisabled) return false;
            if (_interstitial == null) return false;

            if (!CanShowInterstitial())
            {
                if (!_interstitial.IsReady)
                    _interstitial.Load();

                return false;
            }

            // 표시 시도
            if (!_interstitial.Show())
                return false;

            _lastInterstitialUtc = DateTime.UtcNow;
            _interstitialCountThisSession++;

            try
            {
                // 광고 한 번의 라이프사이클 종료까지 기다림
                var e = await EventBusAsyncUtil.WaitFor<InterstitialFinishedEvent>(
                    predicate: null,
                    cancellationToken: ct
                );

                // e.Succeeded == true 인 경우에만 true
                return e.Succeeded;
            }
            catch (OperationCanceledException)
            {
                // 취소되면 false 처리 (상황에 따라 throw 해도 됨)
                return false;
            }
        }

        public async UniTask<bool> ShowRewardedAsync(CancellationToken ct = default)
        {
            if (_state != AdsServiceState.Configured) return false;
            if (_runtimeDisabled) return false;
            if (_rewarded == null) return false;

            if (!_rewarded.IsReady)
                _rewarded.Load();

            // 기존 콜백 API로는 onReward 안 넘겨도 됨 (보상 여부는 이벤트에서 판단)
            _rewarded.Show(null);

            try
            {
                var e = await EventBusAsyncUtil.WaitFor<RewardedFinishedEvent>(
                    predicate: null,
                    cancellationToken: ct
                );

                // 광고가 정상적으로 끝났고 + 실제 보상까지 지급된 경우에만 true
                return e.Succeeded && e.RewardGranted;
            }
            catch (OperationCanceledException) { return false; }
        }
        
        public async UniTask<bool> WaitForAppOpenReadyAsync(
            float maxWaitSeconds,
            CancellationToken ct = default)
        {
            if (_state != AdsServiceState.Configured)
                return false;
            if (_runtimeDisabled)
                return false;
            if (_appOpen == null)
                return false;

            float start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < maxWaitSeconds)
            {
                if (ct.IsCancellationRequested)
                    return false;

                if (_appOpen.IsReady)
                    return true;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            // 타임아웃 시 마지막 상태 한 번 더 확인
            return _appOpen.IsReady;
        }

        public bool TryShowAppOpen()
        {
            if (_runtimeDisabled) return false;
            if (_appOpen == null || !_appOpen.IsReady) return false;

            _appOpen.ShowIfReady();
            return true;
        }

    }
}