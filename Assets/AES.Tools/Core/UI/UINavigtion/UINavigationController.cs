using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 여러 ScreenNavigator 간 전환 + Nav/Screen 단위 Back 규칙 통합
/// </summary>
public class UINavigationController : MonoBehaviour
{
    [Header("이 컨트롤러가 검색할 루트 (생략 시 자기 자신)")]
    [SerializeField] private Transform scanRoot;

    [Header("시작 시 사용할 루트 Navigator (생략 시 첫 번째)")]
    [SerializeField] private UINavigator rootNavigator;

    private readonly Dictionary<Type, UINavigator> _cache = new();
    private readonly Stack<UINavigator> _navStack = new();

    public UINavigator CurrentNavigator => _navStack.Count > 0 ? _navStack.Peek() : null;

    private void Awake()
    {
        if (scanRoot == null)
            scanRoot = transform;

        // 씬 구조 기준 스캔 (최적 스캔: 지정 루트 아래만 검색)
        var navs = scanRoot.GetComponentsInChildren<UINavigator>(true);
        foreach (var nav in navs)
        {
            _cache[nav.GetType()] = nav;
            nav.gameObject.SetActive(false);
        }

        if (rootNavigator == null && navs.Length > 0)
            rootNavigator = navs[0];

        if (rootNavigator != null)
        {
            SwitchTo(rootNavigator.GetType(), pushToStack: true);
        }
    }

    /// <summary>
    /// 타입 기반 Navigator 전환
    /// </summary>
    public TNav SwitchTo<TNav>() where TNav : UINavigator
    {
        return (TNav)SwitchTo(typeof(TNav), pushToStack: true);
    }

    /// <summary>
    /// 글로벌 Back 규칙
    /// 1) 현재 Navigator 에서 Pop 가능한 Screen 이 있으면 Screen Pop
    /// 2) 아니면 Navigator 스택 Pop (이전 Navigator 로 복귀)
    /// 3) 더 이상 없으면 아무것도 하지 않음
    /// </summary>
    public void Back()
    {
        var current = CurrentNavigator;
        if (current == null)
            return;

        // 1) 우선 Screen 단위에서 뒤로가기
        if (current.CanPopScreen)
        {
            current.Pop();
            return;
        }

        // 2) Screen 이 더 이상 없으면 Navigator 단위 뒤로가기
        if (_navStack.Count <= 1)
        {
            // 더 이상 돌아갈 Nav 없음 (앱 종료 트리거 등은 여기서 처리 가능)
            return;
        }

        var top = _navStack.Pop();
        top.HideAll();
        top.gameObject.SetActive(false);

        var previous = CurrentNavigator;
        previous.gameObject.SetActive(true);
        previous.ShowInitial();
    }

    // =====================================================================
    // 내부 구현
    // =====================================================================

    private UINavigator SwitchTo(Type navType, bool pushToStack)
    {
        if (!_cache.TryGetValue(navType, out var nav) || nav == null)
        {
            Debug.LogWarning($"[UINavigationController] Navigator not found: {navType.Name}", this);
            return null;
        }

        var current = CurrentNavigator;
        if (current == nav)
            return nav;

        if (current != null)
        {
            current.HideAll();
            current.gameObject.SetActive(false);
        }

        nav.gameObject.SetActive(true);

        if (pushToStack || _navStack.Count == 0)
            _navStack.Push(nav);

        nav.ShowInitial();
        return nav;
    }
}
