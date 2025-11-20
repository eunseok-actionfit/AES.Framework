using System;


namespace AES.Tools.Core.Policies
{
    /// <summary>
    /// 동일 UI ID의 인스턴스 생성 정책
    /// </summary>
    public enum UIInstancePolicy
    {
        /// <summary>단일 인스턴스만 허용</summary>
        Singleton,

        /// <summary>여러 인스턴스 허용 (예: 토스트, 팝업 등)</summary>
        Multi,

        /// <summary>기존 인스턴스가 있다면 닫고 새로 교체</summary>
        ReplaceExisting,

        /// <summary>이미 열려 있으면 무시 (열지 않음)</summary>
        DenyIfOpen
    }

    /// <summary>
    /// 동시에 표시될 수 없는 UI 그룹 (Exclusive Layer)
    /// </summary>
    [Flags]
    public enum UIExclusiveGroup
    {
        None = 0, Dialog = 1 << 0, Fullscreen = 1 << 1,
        Tooltip = 1 << 2, HUD = 1 << 3
    }

    /// <summary>
    /// UI 닫힘 트리거 조건
    /// </summary>
    public enum UICloseOn
    {
        None, // 수동 닫기만 허용
        BackKey, // 백키 입력 시 닫힘
        ClickOutside, // 바깥 클릭 시 닫힘
        BackOrOutside // 백키 또는 바깥 클릭 시 닫힘
    }

    /// <summary>
    /// 트랜지션 중 중복 Show 호출 시의 처리 정책
    /// </summary>
    public enum UIConcurrency
    {
        Queue, // 현재 트랜지션 완료 후 실행
        Ignore, // 재호출 무시
        Replace, // 기존 표시 중인 뷰를 즉시 교체
        UpdateModel // 현재 인스턴스에 모델만 갱신
    }



}