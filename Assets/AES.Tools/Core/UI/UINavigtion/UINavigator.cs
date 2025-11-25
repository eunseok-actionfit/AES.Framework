using System;
using System.Collections.Generic;
using AES.Tools.View;
using UnityEngine;

#if ODIN_INSPECTOR
using ReadOnly = Sirenix.OdinInspector.ReadOnlyAttribute;


#else
using ReadOnly = AES.Tools.ReadOnlyAttribute;
#endif



public class UINavigator : MonoBehaviour
{
    [SerializeField] private Transform scanRoot;

    [SerializeField] private UIView rootScreen;

    private List<UIView> screens = new();
    private readonly Dictionary<Type, UIView> _cache = new();
    private readonly Stack<UIView> _stack = new();

    public UIView Current => _stack.Count > 0 ? _stack.Peek() : null;
    public bool CanPopScreen => _stack.Count > 1;

    private bool _initialized;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (scanRoot == null)
            scanRoot = transform;

        screens.Clear();
        var found = scanRoot.GetComponentsInChildren<UIView>(true);
        screens.AddRange(found);

        // 루트 지정 안 했으면 첫 번째를 루트로
        if (rootScreen == null && screens.Count > 0)
            rootScreen = screens[0];
    }
#endif

    private void Awake()
    {
        if (scanRoot == null)
            scanRoot = transform;

        InitializeScreens();
    }

    private void OnEnable()
    {
        ShowInitial();
    }

    private void InitializeScreens()
    {
        if (_initialized) return;
        _initialized = true;

        // OnValidate 에서 채워진 screens 기준으로 초기화
        foreach (var s in screens)
        {
            if (s == null) continue;

            var type = s.GetType();
            _cache.TryAdd(type, s);

            if (s != rootScreen)
                s.Hide(); // 초기 비활성화 책임은 Navigator

            s.transform.localPosition = Vector3.zero;
        }

        _stack.Clear();

        if (rootScreen != null)
        {
            rootScreen.Show();
            _stack.Push(rootScreen);
        }
    }


    /// <summary>
    /// 타입 기반 화면 Push
    /// </summary>
    public T Push<T>() where T : UIView
    {
        var view = GetOrCreate<T>();
        if (view == null)
            return null;

        if (Current != null)
            Current.Hide();

        _stack.Push(view);
        view.Show();
        return view;
    }

    /// <summary>
    /// 현재 화면 Pop (이전 화면 복원)
    /// </summary>
    public void Pop()
    {
        if (!CanPopScreen)
            return;

        var top = _stack.Pop();
        if (top != null)
            top.Hide();

        if (Current != null)
            Current.Show();
    }

    /// <summary>
    /// 이 Navigator 안에서 루트 Screen 으로 복귀
    /// </summary>
    public void GoRoot()
    {
        if (rootScreen == null)
            return;

        ShowInternal(rootScreen, clearStack: true);
    }

    /// <summary>
    /// 이 Navigator 가 관리하는 모든 Screen 숨기기
    /// (Navigator 전환 시 호출)
    /// </summary>
    public void HideAll()
    {
        foreach (var v in _stack)
        {
            if (v != null)
                v.Hide();
        }
    }

    /// <summary>
    /// UINavigationController 가 이 Navigator 를 활성화할 때 호출
    /// - 스택이 비어 있으면 루트 Screen 또는 첫 Screen 을 보여준다.
    /// - 스택이 있으면 최상단 Screen 을 다시 보여준다.
    /// </summary>
    public void ShowInitial()
    {
        if (_stack.Count == 0)
        {
            if (rootScreen != null) { ShowInternal(rootScreen, clearStack: true); }
            else
            {
                // 루트 미지정 시, scanRoot 아래 첫 ScreenView 를 루트로 사용
                var first = scanRoot.GetComponentInChildren<UIView>(true);
                if (first != null)
                    ShowInternal(first, clearStack: true);
            }
        }
        else
        {
            if (Current != null)
                Current.Show();
        }
    }

    // =====================================================================
    // 내부 구현
    // =====================================================================

    private T GetOrCreate<T>() where T : UIView
    {
        var type = typeof(T);

        // 루트 Screen 타입이면 항상 루트 우선
        if (rootScreen != null && rootScreen.GetType() == type)
            return (T)rootScreen;

        if (_cache.TryGetValue(type, out var cached) && cached != null)
            return (T)cached;

        var view = scanRoot.GetComponentInChildren<T>(true);

        if (view == null)
        {
            Debug.LogError($"[ScreenNavigator] ScreenView not found: {type.Name}", this);
            return null;
        }

        _cache[type] = view;
        return view;
    }

    private void ShowInternal(UIView view, bool clearStack)
    {
        if (view == null)
            return;

        foreach (var v in _stack)
        {
            if (v != null)
                v.Hide();
        }

        if (clearStack)
            _stack.Clear();

        _stack.Push(view);
        view.Show();
    }
}