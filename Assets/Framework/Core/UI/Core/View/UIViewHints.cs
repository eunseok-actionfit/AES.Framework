// Assets/Framework/Systems/UI/Core/UIViewHints.cs
using System;
using AES.Tools.Core.Policies;
using UnityEngine;


namespace AES.Tools.Core.View
{
    /// <summary>bool 기반 UI 힌트 (enabled일 때만 value 사용)</summary>
    [Serializable]
    public struct BoolHint
    {
        public bool enabled;
        public bool value;

        public bool Resolve(bool @default) => enabled ? value : @default;
    }

    /// <summary>닫힘 조건 오버라이드용 힌트</summary>
    [Serializable]
    public struct OptionalCloseOn
    {
        public bool enabled;
        public UICloseOn value;

        public UICloseOn Resolve(UICloseOn @default) => enabled ? value : @default;
    }

    /// <summary>int 기반 UI 힌트 (enabled일 때만 value 사용)</summary>
    [Serializable]
    public struct IntHint
    {
        public bool enabled;
        public int value;

        public int Resolve(int @default) => enabled ? value : @default;
    }

    /// <summary>
    /// UIView 인스턴스 표현 제어 힌트 모음.
    /// 생성·스택·레이어 정책은 UIRegistrySO가 소유한다.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/UIView Hints")]
    public sealed class UIViewHints : MonoBehaviour
    {
        [Header("Close / Blocker")]
        [Tooltip("닫힘 조건 오버라이드 (기본 BackKey, ClickOutside 등 무시하고 이 값으로 대체)")]
        public OptionalCloseOn closeOn;

        [Tooltip("입력 차단 오버라이드 (true면 UI 뒤의 입력 차단, false면 통과)")]
        public BoolHint inputBlocker;

        [Header("Safe Area")]
        [Tooltip("SafeArea(노치 영역) 적용 여부 오버라이드")]
        public BoolHint useSafeArea;

        [Tooltip("SafeArea 미사용 시 전체 화면으로 스트레칭할지 여부")]
        public BoolHint stretchFullWhenNoSafe;

        [Header("Extra Insets (px)")]
        [Tooltip("좌측 여백(px) 추가 (SafeArea 계산 후 더해짐)")]
        public IntHint extraLeft;

        [Tooltip("우측 여백(px) 추가 (SafeArea 계산 후 더해짐)")]
        public IntHint extraRight;

        [Tooltip("상단 여백(px) 추가 (SafeArea 계산 후 더해짐)")]
        public IntHint extraTop;

        [Tooltip("하단 여백(px) 추가 (SafeArea 계산 후 더해짐)")]
        public IntHint extraBottom;
    }
}
