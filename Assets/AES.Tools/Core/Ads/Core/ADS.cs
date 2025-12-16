using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;


namespace AES.Tools.VContainer
{
    public static class ADS
    {
        private static IAdsService _service;

        /// <summary> AdsModule에서 1회 Bind </summary>
        internal static void Bind(IAdsService service)
        {
            _service = service;
        }

        public static bool IsBound => _service != null;

        public static bool IsReadyInterstitial
            => _service?.IsReadyInterstitial ?? false;

        public static bool IsReadyRewarded
            => _service?.IsReadyRewarded ?? false;

        public static bool IsReadyAppOpen
            => _service?.IsReadyAppOpen ?? false;



        public static void SetRuntimeDisabled(bool disabled)
            => _service?.SetRuntimeDisabled(disabled);

        // ================= Banner =================
        public static void ShowBanner()
            => _service?.ShowBanner();

        public static void HideBanner()
            => _service?.HideBanner();

        // ================= Interstitial =================
        public static bool TryShowInterstitial(string reason)
            => (_service as AdsService)?.TryShowInterstitial(reason) ?? false;

        // ================= Rewarded =================
        public static bool TryShowRewarded(string reason, Action onReward)
            => (_service as AdsService)?.TryShowRewarded(reason, onReward) ?? false;

        // ================= AppOpen =================
        public static bool TryShowAppOpen(string reason)
            => (_service as AdsService)?.TryShowAppOpen(reason) ?? false;

        // ================= Sensitive Flow =================
        public static void NotifySensitiveFlowStarted()
            => (_service as AdsService)?.NotifySensitiveFlowStarted();

        public static void NotifySensitiveFlowEnded()
            => (_service as AdsService)?.NotifySensitiveFlowEnded();

        public static void MarkAppReadyForAppOpen()
            => (_service as AdsService)?.MarkAppReadyForAppOpen();

        public static UniTask<bool> ShowRewardedAsync(string reason, CancellationToken ct = default)
        {
            if (_service == null)
                return UniTask.FromResult(false);

            return (_service as AdsService)?.ShowRewardedAsync(reason, ct) ?? UniTask.FromResult(false);
        }
    }
}