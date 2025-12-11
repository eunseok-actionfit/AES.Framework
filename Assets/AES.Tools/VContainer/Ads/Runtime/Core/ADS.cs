using System;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools.VContainer
{
    public static class ADS
    {
        private static IAdsService _service;

        /// <summary>
        /// AdsModule에서 한 번만 Bind.
        /// </summary>
        internal static void Bind(IAdsService service)
        {
            _service = service;
        }

        /// <summary>
        /// 서비스가 실제로 바인딩되어 있는지 (디버그/안전 체크용)
        /// </summary>
        public static bool IsBound => _service != null;

        /// <summary>
        /// 런타임 광고 ON/OFF (IAP 광고제거, RemoteConfig 등에서 사용)
        /// </summary>
        public static void SetRuntimeDisabled(bool disabled)
            => _service?.SetRuntimeDisabled(disabled);

        // -------------------------------
        // 배너
        // -------------------------------
        public static void ShowBanner() => _service?.ShowBanner();
        public static void HideBanner() => _service?.HideBanner();

        // -------------------------------
        // 전면 광고
        // -------------------------------
        public static bool IsReadyInterstitial
            => _service?.IsReadyInterstitial ?? false;

        public static void ShowInterstitial()
            => _service?.ShowInterstitial();

        /// <summary>
        /// 준비 안 됐으면 아무것도 안 하고 false 반환.
        /// </summary>
        public static bool TryShowInterstitial()
        {
            if (_service == null) return false;
            if (!_service.IsReadyInterstitial) return false;

            _service.ShowInterstitial();
            return true;
        }
        
        public static UniTask<bool> ShowInterstitialAsync(CancellationToken ct = default)
        {
            if (_service == null)
                return UniTask.FromResult(false);

            return _service.ShowInterstitialAsync(ct);
        }

        // -------------------------------
        // 보상 광고
        // -------------------------------
        public static bool IsReadyRewarded
            => _service?.IsReadyRewarded ?? false;

        public static void ShowRewarded(Action onReward)
            => _service?.ShowRewarded(onReward);

        /// <summary>
        /// 준비 안 됐으면 onFail 호출 없이 false만 반환.
        /// 필요하면 onFail 파라미터 추가해서 커스텀 UX도 가능.
        /// </summary>
        public static bool TryShowRewarded(Action onReward)
        {
            if (_service == null) return false;
            if (!_service.IsReadyRewarded) return false;

            _service.ShowRewarded(onReward);
            return true;
        }
        
        public static UniTask<bool> ShowRewardedAsync(CancellationToken ct = default)
        {
            if (_service == null)
                return UniTask.FromResult(false);

            return _service.ShowRewardedAsync(ct);
        }
        
        // -------------------------------
        // AppOpen 상태/제어
        // -------------------------------
        public static bool IsReadyAppOpen
            => _service?.IsReadyAppOpen ?? false;

        public static UniTask<bool> WaitForAppOpenReadyAsync(
            float maxWaitSeconds,
            CancellationToken ct = default)
        {
            if (_service == null)
                return UniTask.FromResult(false);

            return _service.WaitForAppOpenReadyAsync(maxWaitSeconds, ct);
        }

        public static bool TryShowAppOpen()
        {
            if (_service == null) return false;
            return _service.TryShowAppOpen();
        }
    }
}
