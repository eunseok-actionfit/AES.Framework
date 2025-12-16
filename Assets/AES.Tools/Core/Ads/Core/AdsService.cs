using System;
using System.Threading;
using AES.Tools.TimeManager.Schedulers;
using AES.Tools.VContainer.AppLifetime;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AES.Tools.VContainer
{
    public sealed class AdsService : IAdsService, IDisposable
    {
        // =========================================================
        // Gate (정책 주입 포인트)
        // =========================================================
        public Func<bool> CanShowBannerGate;
        public Func<string, bool> CanShowInterstitialGate;
        public Func<string, bool> CanShowRewardedGate;
        public Func<string, bool> CanShowAppOpenGate;

        // =========================================================
        // Runtime / State
        // =========================================================
        private bool _runtimeDisabled;
        private bool _appReadyForAppOpen;
        private int _sensitiveDepth;

        private DateTime _lastInterstitialUtc;
        private int _interstitialCountThisSession;

        private DateTime _lastAppOpenShownUtc;
        private DateTime _lastSensitiveEndedUtc;

        private DateTime _appOpenBlockUntilUtc;
        private int _appOpenBlockDepth;

        private AdsServiceState _state = AdsServiceState.Uninitialized;

        // =========================================================
        // Dependencies / Providers
        // =========================================================
        private readonly ITimerScheduler _scheduler;
        private readonly AdsRuntimeSettings _settings;
        private readonly IAdsProviderFactory _factory;

        private IAppOpenAdService _appOpen;
        private IInterstitialAdService _interstitial;
        private IRewardedAdService _rewarded;
        private IBannerAdService _banner;

        // =========================================================
        // Events
        // =========================================================
        private EventBinding<ApplicationFocusChangedEvent> _focusBinding;
        private EventBinding<ApplicationPauseChangedEvent> _pauseBinding;
        private EventBinding<ApplicationQuitEvent> _quitBinding;

        // =========================================================
        // IAdsService Properties
        // =========================================================
        public bool IsReadyInterstitial => !_runtimeDisabled && (_interstitial?.IsReady ?? false);
        public bool IsReadyRewarded => !_runtimeDisabled && (_rewarded?.IsReady ?? false);
        public bool IsReadyAppOpen => !_runtimeDisabled && (_appOpen?.IsReady ?? false);

        // =========================================================
        // Constructor
        // =========================================================
        public AdsService(
            ITimerScheduler scheduler,
            AdsRuntimeSettings settings,
            IAdsProviderFactory factory)
        {
            _scheduler = scheduler;
            _settings = settings;
            _factory = factory;
        }

        // =========================================================
        // Configure / Dispose
        // =========================================================
        public void Configure(AdsProfile profile)
        {
            if (_state == AdsServiceState.Configured)
                return;

            _state = AdsServiceState.Configured;

            _appOpen = _factory.CreateAppOpen(profile);
            _interstitial = _factory.CreateInterstitial(profile, _scheduler);
            _rewarded = _factory.CreateRewarded(profile, _scheduler);
            _banner = _factory.CreateBanner(profile);

            _appOpen?.Initialize(); _appOpen?.Load();
            _interstitial?.Initialize(); _interstitial?.Load();
            _rewarded?.Initialize(); _rewarded?.Load();
            _banner?.Initialize();

            BindLifecycleEvents();
        }

        public void Dispose()
        {
            if (_state == AdsServiceState.Disposed)
                return;

            _state = AdsServiceState.Disposed;

            _focusBinding?.Deregister();
            _pauseBinding?.Deregister();
            _quitBinding?.Deregister();

            (_appOpen as IDisposable)?.Dispose();
            (_interstitial as IDisposable)?.Dispose();
            (_rewarded as IDisposable)?.Dispose();
            (_banner as IDisposable)?.Dispose();
        }

        // =========================================================
        // Banner
        // =========================================================
        public void ShowBanner()
        {
            if (_runtimeDisabled) return;
            if (CanShowBannerGate != null && !CanShowBannerGate()) return;

            _banner?.Show();
        }

        public void HideBanner()
        {
            _banner?.Hide();
        }

        // =========================================================
        // Interstitial
        // =========================================================
        public void ShowInterstitial()
        {
            TryShowInterstitial(string.Empty);
        }

        public bool TryShowInterstitial(string reason)
        {
            if (_runtimeDisabled) return false;
            if (_interstitial == null || !_interstitial.IsReady) return false;

            if (CanShowInterstitialGate != null &&
                !CanShowInterstitialGate(reason))
                return false;

            if (!CanShowInterstitialInternal())
                return false;

            if (_interstitial.Show())
            {
                _lastInterstitialUtc = DateTime.UtcNow;
                _interstitialCountThisSession++;
                return true;
            }

            return false;
        }

        public async UniTask<bool> ShowInterstitialAsync(CancellationToken ct = default)
        {
            if (!TryShowInterstitial(string.Empty))
                return false;

            try
            {
                var e = await EventBusAsyncUtil.WaitFor<InterstitialFinishedEvent>(
                    null, ct);
                return e.Succeeded;
            }
            catch
            {
                return false;
            }
        }

        private bool CanShowInterstitialInternal()
        {
            var interval = TimeSpan.FromSeconds(
                Mathf.Max(0f, _settings.interstitialMinIntervalSeconds));

            if (DateTime.UtcNow - _lastInterstitialUtc < interval)
                return false;

            if (_settings.interstitialMaxPerSession > 0 &&
                _interstitialCountThisSession >= _settings.interstitialMaxPerSession)
                return false;

            return true;
        }

        // =========================================================
        // Rewarded
        // =========================================================
        public void ShowRewarded(Action onReward)
        {
            TryShowRewarded(string.Empty, onReward);
        }

        public bool TryShowRewarded(string reason, Action onReward)
        {
            if (_runtimeDisabled) return false;
            if (_rewarded == null || !_rewarded.IsReady) return false;

            if (CanShowRewardedGate != null &&
                !CanShowRewardedGate(reason))
                return false;

            _rewarded.Show(onReward);
            return true;
        }

        public async UniTask<bool> ShowRewardedAsync(CancellationToken ct = default)
        {
            if (_rewarded == null || !_rewarded.IsReady)
                return false;

            _rewarded.Show(null);

            try
            {
                var e = await EventBusAsyncUtil.WaitFor<RewardedFinishedEvent>(
                    null, ct);
                return e.Succeeded && e.RewardGranted;
            }
            catch
            {
                return false;
            }
        }
        
        public async UniTask<bool> ShowRewardedAsync(string reason, CancellationToken ct = default)
        {
            if (_runtimeDisabled) return false;
            if (_rewarded == null || !_rewarded.IsReady) return false;

            if (CanShowRewardedGate != null &&
                !CanShowRewardedGate(reason))
                return false;

            _rewarded.Show(null);

            try
            {
                var e = await EventBusAsyncUtil.WaitFor<RewardedFinishedEvent>(null, ct);
                return e.Succeeded && e.RewardGranted;
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // AppOpen
        // =========================================================
        public bool TryShowAppOpen()
        {
            return TryShowAppOpen(string.Empty);
        }

        public bool TryShowAppOpen(string reason)
        {
            if (_runtimeDisabled) return false;
            if (!_appReadyForAppOpen) return false;
            if (_appOpen == null || !_appOpen.IsReady) return false;
            if (IsAppOpenBlockedNow()) return false;

            if (CanShowAppOpenGate != null &&
                !CanShowAppOpenGate(reason))
                return false;

            _lastAppOpenShownUtc = DateTime.UtcNow;
            _appOpen.ShowIfReady();
            return true;
        }

        public UniTask<bool> WaitForAppOpenReadyAsync(
            float maxWaitSeconds,
            CancellationToken ct = default)
        {
            return UniTask.FromResult(IsReadyAppOpen);
        }

        // =========================================================
        // AppOpen Control / Sensitive Flow
        // =========================================================
        public void NotifySensitiveFlowStarted()
        {
            _sensitiveDepth++;
        }

        public void NotifySensitiveFlowEnded()
        {
            if (_sensitiveDepth > 0)
                _sensitiveDepth--;

            _lastSensitiveEndedUtc = DateTime.UtcNow;
        }

        public void PushAppOpenBlock()
        {
            _appOpenBlockDepth++;
        }

        public void PopAppOpenBlock()
        {
            _appOpenBlockDepth = Mathf.Max(0, _appOpenBlockDepth - 1);
        }

        public void BlockAppOpenForSeconds(float seconds)
        {
            _appOpenBlockUntilUtc = DateTime.UtcNow.AddSeconds(seconds);
        }

        public void MarkAppReadyForAppOpen()
        {
            _appReadyForAppOpen = true;
        }

        private bool IsAppOpenBlockedNow()
        {
            if (_appOpenBlockDepth > 0)
                return true;

            if (_appOpenBlockUntilUtc != default &&
                DateTime.UtcNow < _appOpenBlockUntilUtc)
                return true;

            return false;
        }

        // =========================================================
        // Runtime
        // =========================================================
        public void SetRuntimeDisabled(bool disabled)
        {
            _runtimeDisabled = disabled;
            if (disabled)
                _banner?.Hide();
        }

        // =========================================================
        // Lifecycle Bindings
        // =========================================================
        private void BindLifecycleEvents()
        {
            _focusBinding = new EventBinding<ApplicationFocusChangedEvent>()
                .Add(e =>
                {
                    if (e.Focused && !_runtimeDisabled)
                        TryShowAppOpen("resume");
                })
                .Register();

            _pauseBinding = new EventBinding<ApplicationPauseChangedEvent>()
                .Add(e =>
                {
                    if (!e.Paused && !_runtimeDisabled)
                        TryShowAppOpen("resume");
                })
                .Register();

            _quitBinding = new EventBinding<ApplicationQuitEvent>()
                .Add(_ => Dispose())
                .Register();
        }
    }

    public enum AdsServiceState
    {
        Uninitialized,
        Configured,
        Disposed
    }
}
