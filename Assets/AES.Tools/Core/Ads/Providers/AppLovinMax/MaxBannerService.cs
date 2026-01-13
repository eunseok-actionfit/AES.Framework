#if AESFW_ADS_MAX
using System;
using AES.Tools.UI;
using UnityEngine;

namespace AES.Tools.VContainer
{
    public class MaxBannerService : IBannerAdService, IDisposable
    {
        private readonly string _unitId;
        private bool _created;
        private bool _callbacksRegistered;
        private MaxSdk.AdViewPosition _position;

        private int _lastHeightPx;   // 마지막으로 알던 배너 높이
        private bool _isVisible;     // 마지막으로 알던 표시 상태

        public MaxBannerService(string unitId, MaxSdk.AdViewPosition position = MaxSdk.AdViewPosition.BottomCenter)
        {
            _unitId   = unitId;
            _position = position;
        }

        public void Initialize()
        {
            RegisterCallbacksOnce();
        }

        public void Show()
        {
            if (string.IsNullOrEmpty(_unitId))
            {
                Debug.LogWarning("[MaxBanner] Unit id is empty.");
                return;
            }

            RegisterCallbacksOnce();

            if (!_created)
            {
                var config = new MaxSdk.AdViewConfiguration(_position);
                MaxSdk.CreateBanner(_unitId, config);
                _created = true;
            }

            // 1) Show 호출 시점에도 UI는 즉시 펼치기 (height는 캐시값/예상값)
            if (_lastHeightPx <= 0)
                _lastHeightPx = DpToPx(50); // 기본 추정(배너)

            Publish(_lastHeightPx, true);

            MaxSdk.ShowBanner(_unitId);
        }

        public void Hide()
        {
            if (_created)
                MaxSdk.HideBanner(_unitId);

            // 2) Hide 호출 시점 즉시 UI 접기
            Publish(0, false);
        }

        public void Dispose()
        {
            UnregisterCallbacks();

            if (_created)
            {
                MaxSdk.DestroyBanner(_unitId);
                _created = false;
            }

            Publish(0, false);
        }

        private void Publish(int heightPx, bool visible)
        {
            _lastHeightPx = heightPx;
            _isVisible = visible;
            EventBus<BannerHeightChangedEvent>.Raise(new BannerHeightChangedEvent(heightPx, visible));
        }

        private void RegisterCallbacksOnce()
        {
            if (_callbacksRegistered) return;
            _callbacksRegistered = true;

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerLoadFailed;
        }

        private void UnregisterCallbacks()
        {
            if (!_callbacksRegistered) return;
            _callbacksRegistered = false;

            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerLoadFailed;
        }

        private void OnBannerLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _unitId) return;

            // 3) 로드 완료 시점에 정확(또는 더 근접)한 값으로 갱신
            int heightPx = ResolveBannerHeightPx(adInfo);
            Publish(heightPx, true);
        }

        private void OnBannerLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != _unitId) return;

            Debug.LogWarning($"[MaxBannerService] Banner failed to load: {errorInfo?.Message}");

            // 로드 실패면 표시 불가로 처리
            Publish(0, false);
        }

        private int ResolveBannerHeightPx(MaxSdkBase.AdInfo adInfo)
        {
            // 버전차 대응: 문자열 비교
            string format = adInfo.AdFormat;

            int heightDp = format switch
            {
                "BANNER" => 50,
                "LEADER" => 90,
                "MREC"   => 250,
                _        => 50
            };

            return DpToPx(heightDp);
        }

        private int DpToPx(int dp)
        {
            float dpi = Screen.dpi;
            if (dpi <= 0f) dpi = 160f;
            return Mathf.RoundToInt(dp * (dpi / 160f));
        }
    }
}
#endif
