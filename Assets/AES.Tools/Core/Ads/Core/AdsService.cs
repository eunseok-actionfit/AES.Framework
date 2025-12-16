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
    [Serializable]
    public sealed class AdsRuntimeSettings
    {
        public float interstitialMinIntervalSeconds = 40f;
        public int interstitialMaxPerSession = 10;
        public bool runtimeAdsDisabled = false;
    }

    public enum AdsServiceState { Uninitialized, Configured, Disposed }

    public class AdsService : IAdsService
    {
        private readonly ITimerScheduler _scheduler;
        private readonly IAdsProviderFactory _factory;

        private readonly AdsRuntimeSettings _settings;
        private readonly TestDeviceFlags _testDeviceFlags;

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

        private DateTime _lastInterstitialUtc;
        private int _interstitialCountThisSession;

        private bool _runtimeDisabled;

        public bool IsReadyInterstitial => !_runtimeDisabled && (_interstitial?.IsReady ?? false);
        public bool IsReadyRewarded => !_runtimeDisabled && (_rewarded?.IsReady ?? false);
        public bool IsReadyAppOpen => _appOpen != null && _appOpen.IsReady;

     
        public AdsService(ITimerScheduler scheduler, AdsRuntimeSettings settings, TestDeviceFlags testDeviceFlags, IAdsProviderFactory factory)
        {
            _scheduler = scheduler;
            _settings = settings ?? new AdsRuntimeSettings();
            _testDeviceFlags = testDeviceFlags;
            _factory = factory;

            // 우선순위:
            // 1) CSV 기반 테스트 디바이스 adsDisabled
            // 2) Feature 설정 runtimeAdsDisabled
            var csvAdsDisabled = _testDeviceFlags != null && _testDeviceFlags.adsDisabled;
            var cfgAdsDisabled = _settings.runtimeAdsDisabled;

            _runtimeDisabled = csvAdsDisabled || cfgAdsDisabled;

#if UNITY_EDITOR
            if (_testDeviceFlags != null && _testDeviceFlags.isTester)
            {
                Debug.Log($"[AdsService] Test device detected: name={_testDeviceFlags.matchedName}, id={_testDeviceFlags.matchedDeviceId}, adsDisabled={_runtimeDisabled}");
            }
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
            _appOpen       = _factory.CreateAppOpen(_profile);
            _interstitial  = _factory.CreateInterstitial(_profile, _scheduler);
            _rewarded      = _factory.CreateRewarded(_profile, _scheduler);
            _banner        = _factory.CreateBanner(_profile);

            _appOpen?.Initialize(); _appOpen?.Load();
            _interstitial?.Initialize(); _interstitial?.Load();
            _rewarded?.Initialize(); _rewarded?.Load();
            _banner?.Initialize();
        }

        private void SetupPauseBinding()
        {
            _pauseBinding = new EventBinding<ApplicationPauseChangedEvent>()
                .Add(e =>
                {
                    var ready = IsReadyAppOpen;

                    Debug.Log($"[AdsService] Paused={e.Paused}, ready={ready}, runtimeDisabled={_runtimeDisabled}");

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
                .Add(e =>
                {
                    var ready = IsReadyAppOpen;

                    Debug.Log($"[AdsService] Focus={e.Focused}, ready={ready}, runtimeDisabled={_runtimeDisabled}");

                    if (!e.Focused) return;
                    if (_runtimeDisabled) return;

                    if (!ready)
                    {
                        Debug.Log("[AdsService] AppOpen NOT ready at focus. (skip)");
                        if (_appOpen != null) _appOpen.Load();
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
        

        public void ShowBanner()
        {
            if (_state != AdsServiceState.Configured) return;
            if (_runtimeDisabled) return;

            _banner?.Show();

            int bannerHeightPx = 0;

            switch (_bannerNetwork)
            {
                case AdNetworkType.AdMob:
                    bannerHeightPx = BannerHeightUtil.GetAdmobAdaptiveBannerHeightPx();
                    break;
                case AdNetworkType.AppLovinMax:
                    bannerHeightPx = BannerHeightUtil.GetMaxAdaptiveBannerHeightPx();
                    break;
                default:
                    bannerHeightPx = BannerHeightUtil.GetDefaultBannerHeightPx();
                    break;
            }

            EventBus<BannerHeightChangedEvent>.Raise(new BannerHeightChangedEvent(bannerHeightPx, true));
        }

        public void HideBanner()
        {
            if (_state != AdsServiceState.Configured) return;

            _banner?.Hide();
            EventBus<BannerHeightChangedEvent>.Raise(new BannerHeightChangedEvent(0, false));
        }

        private bool CanShowInterstitial()
        {
            if (_runtimeDisabled) return false;
            if (_interstitial == null || !_interstitial.IsReady) return false;

            var interval = TimeSpan.FromSeconds(Mathf.Max(0f, _settings.interstitialMinIntervalSeconds));
            if (DateTime.UtcNow - _lastInterstitialUtc < interval)
                return false;

            if (_settings.interstitialMaxPerSession > 0 &&
                _interstitialCountThisSession >= _settings.interstitialMaxPerSession)
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

            if (!_interstitial.Show())
                return false;

            _lastInterstitialUtc = DateTime.UtcNow;
            _interstitialCountThisSession++;

            try
            {
                var e = await EventBusAsyncUtil.WaitFor<InterstitialFinishedEvent>(
                    predicate: null,
                    cancellationToken: ct
                );
                return e.Succeeded;
            }
            catch (OperationCanceledException) { return false; }
        }

        public async UniTask<bool> ShowRewardedAsync(CancellationToken ct = default)
        {
            if (_state != AdsServiceState.Configured) return false;
            if (_runtimeDisabled) return false;
            if (_rewarded == null) return false;

            if (!_rewarded.IsReady)
                _rewarded.Load();

            _rewarded.Show(null);

            try
            {
                var e = await EventBusAsyncUtil.WaitFor<RewardedFinishedEvent>(
                    predicate: null,
                    cancellationToken: ct
                );

                return e.Succeeded && e.RewardGranted;
            }
            catch (OperationCanceledException) { return false; }
        }

        public async UniTask<bool> WaitForAppOpenReadyAsync(float maxWaitSeconds, CancellationToken ct = default)
        {
            if (_state != AdsServiceState.Configured) return false;
            if (_runtimeDisabled) return false;
            if (_appOpen == null) return false;

            float start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < maxWaitSeconds)
            {
                if (ct.IsCancellationRequested)
                    return false;

                if (_appOpen.IsReady)
                    return true;

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

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
