using System;
using UnityEngine;

public class MaxBannerService : IBannerAdService, IDisposable
{
    private readonly string _unitId;
    private bool _created;
    private MaxSdk.AdViewPosition _position;

    /// 

    /// position 파라미터로 배너 위치 지정 (기본값 BottomCenter)
    /// 

    public MaxBannerService(string unitId, MaxSdk.AdViewPosition position = MaxSdk.AdViewPosition.BottomCenter)
    {
        _unitId = unitId;
        _position = position;
    }

    public void Initialize()
    {
        // 앱 전역에서 MaxSdk.InitializeSdk()는 반드시 1회만 호출. 이 서비스에서는 불필요.
    }

    /// 

    /// 배너를 로드 및 화면에 표시합니다.
    /// 

    public void LoadAndShow()
    {
        if (string.IsNullOrEmpty(_unitId))
        {
            Debug.LogWarning("[MaxBanner] Unit id is empty.");
            return;
        }

        if (!_created)
        {
            var config = new MaxSdk.AdViewConfiguration(_position);
            MaxSdk.CreateBanner(_unitId, config);

            // (옵션) 배너 배경색 설정 (투명: #00000000, 투명 미지원 기기에선 대체가능)
            // MaxSdk.SetBannerBackgroundColor(_unitId, "#00000000");

            // (옵션) adaptive banner 끄기
            // MaxSdk.SetBannerExtraParameter(_unitId, "adaptive_banner", "false");

            _created = true;
        }

        MaxSdk.ShowBanner(_unitId);
    }

    /// 

    /// 배너 숨김 (화면 상에서만 숨기며, 기억됨)
    /// 

    public void Hide()
    {
        if (_created)
        {
            MaxSdk.HideBanner(_unitId);
        }
    }

    /// 

    /// 배너를 (초기 생성/로드 포함) 다시 보여줍니다.
    /// 

    public void Show()
    {
        if (!_created)
        {
            LoadAndShow();
        }
        else
        {
            MaxSdk.ShowBanner(_unitId);
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