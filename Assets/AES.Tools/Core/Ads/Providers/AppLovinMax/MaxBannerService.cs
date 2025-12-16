#if AESFW_ADS_MAX
using System;
using UnityEngine;

namespace AES.Tools.VContainer
{
    public class MaxBannerService : IBannerAdService, IDisposable
    {
        private readonly string _unitId;
        private bool _created;
        private MaxSdk.AdViewPosition _position;

        public MaxBannerService(string unitId, MaxSdk.AdViewPosition position = MaxSdk.AdViewPosition.BottomCenter)
        {
            _unitId   = unitId;
            _position = position;
        }

        public void Initialize()
        {
            // MaxSdk.InitializeSdk() 는 앱 전역 1회 호출 가정.
        }

        public void Show()
        {
            if (string.IsNullOrEmpty(_unitId))
            {
                Debug.LogWarning("[MaxBanner] Unit id is empty.");
                return;
            }

            Debug.Log($"[MaxBannerService] Show called. _created={_created}, unitId={_unitId}");

            if (!_created)
            {
                var config = new MaxSdk.AdViewConfiguration(_position);
                MaxSdk.CreateBanner(_unitId, config);

                // (옵션) 배경색
                // MaxSdk.SetBannerBackgroundColor(_unitId, new Color(0.1f, 0.1f, 0.1f, 0f));

                _created = true;
            }

            MaxSdk.ShowBanner(_unitId);
        }

        public void Hide()
        {
            Debug.Log($"[MaxBannerService] Hide called. _created={_created}, unitId={_unitId}");

            if (_created)
            {
                MaxSdk.HideBanner(_unitId);
            }
        }

        public void Dispose()
        {
            if (_created)
            {
                MaxSdk.DestroyBanner(_unitId);
                _created = false;
            }
        }
    }
}
#endif