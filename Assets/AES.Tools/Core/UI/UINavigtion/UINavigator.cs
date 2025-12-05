using System;
using System.Collections.Generic;
using AES.Tools;
using AES.Tools.View;
using UnityEngine;

#if ODIN_INSPECTOR


#else
using ReadOnly = AES.Tools.ReadOnlyAttribute;
#endif

public class UINavigator : MonoBehaviour
{
    [SerializeField] private Transform scanRoot;
    
    [Header("Options")]
    [Tooltip("true 면 '아무 화면도 없는 상태'를 루트로 사용합니다.")]
    [SerializeField] private bool useEmptyAsRoot = false;
    
    [SerializeField, ShowIf(nameof(useEmptyAsRoot), Condition = ShowIfCondition.BoolIsFalse)]
    private UIView rootScreen;

    [SerializeField, HideInInspector]private List<UIView> screens = new();
    private readonly Dictionary<Type, UIView> _cache = new();
    private readonly Stack<UIView> _stack = new();

    public UIView Current => _stack.Count > 0 ? _stack.Peek() : null;

    // 루트가 "빈 상태"일 수 있으므로, 스택 기준으로만 Pop 가능 여부 판단
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

        // 루트 자동 지정은 "빈 루트" 모드가 아닐 때만
        if (!useEmptyAsRoot && rootScreen == null && screens.Count > 0)
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

        foreach (var s in screens)
        {
            if (s == null) continue;

            var type = s.GetType();
            _cache.TryAdd(type, s);

            var rt = (RectTransform)s.transform;

            // 부모 기준 전체 확장
            rt.anchorMin = Vector2.zero;          // (0, 0)
            rt.anchorMax = Vector2.one;           // (1, 1)

            // 앵커에서의 여백 0
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 필요하면 스케일/회전도 초기화
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            s.Hide();
        }

        _stack.Clear();

        if (!useEmptyAsRoot && rootScreen != null)
        {
            rootScreen.Show();
            _stack.Push(rootScreen);
        }
        
        // useEmptyAsRoot == true 이면: 아무 화면도 보이지 않는 상태에서 시작
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
        else if (useEmptyAsRoot)
        {
            // 스택이 비었고, 빈 루트 모드면 아무 것도 안 보이는 상태 유지
            HideAll();
        }
    }

    /// <summary>
    /// 이 Navigator 안에서 루트로 복귀
    /// </summary>
    public void GoRoot()
    {
        if (useEmptyAsRoot)
        {
            // 루트 = 아무 화면도 없는 상태
            HideAll();
            _stack.Clear();
            return;
        }

        if (rootScreen != null)
        {
            ShowInternal(rootScreen, clearStack: true);
        }
        else
        {
            // 루트 미지정 시, scanRoot 아래 첫 UIView 를 루트로 사용
            var first = scanRoot != null
                ? scanRoot.GetComponentInChildren<UIView>(true)
                : null;

            if (first != null)
                ShowInternal(first, clearStack: true);
            else
            {
                // 정말 아무것도 없다면 전부 숨기고 스택만 비운다
                HideAll();
                _stack.Clear();
            }
        }
    }

    /// <summary>
    /// 이 Navigator 가 관리하는 모든 Screen 숨기기
    /// (Navigator 전환 시 호출)
    /// </summary>
    public void HideAll()
    {
        // 스택에 올라온 것뿐 아니라, 관리 대상 전체를 숨긴다
        foreach (var v in screens)
        {
            if (v != null)
                v.Hide();
        }
    }

    /// <summary>
    /// UINavigationController 가 이 Navigator 를 활성화할 때 호출
    /// - 스택이 비어 있으면 루트 Screen 또는 첫 Screen, 혹은 빈 루트를 보여준다.
    /// - 스택이 있으면 최상단 Screen 을 다시 보여준다.
    /// </summary>
    public void ShowInitial()
    {
        if (_stack.Count == 0)
        {
            if (useEmptyAsRoot)
            {
                // 빈 루트 모드: 아무 것도 안 보여주는 상태 유지
                HideAll();
                return;
            }

            if (rootScreen != null)
            {
                ShowInternal(rootScreen, clearStack: true);
            }
            else
            {
                // 루트 미지정 시, scanRoot 아래 첫 UIView 를 루트로 사용
                var first = scanRoot != null
                    ? scanRoot.GetComponentInChildren<UIView>(true)
                    : null;

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

        // 루트 Screen 타입이면 항상 루트 우선 (빈 루트 모드가 아니고, rootScreen 이 있을 때만)
        if (!useEmptyAsRoot && rootScreen != null && rootScreen.GetType() == type)
            return (T)rootScreen;

        if (_cache.TryGetValue(type, out var cached) && cached != null)
            return (T)cached;

        var view = scanRoot != null
            ? scanRoot.GetComponentInChildren<T>(true)
            : null;

        if (view == null)
        {
            Debug.LogError($"[UINavigator] UIView not found: {type.Name}", this);
            return null;
        }

        _cache[type] = view;
        return view;
    }

    private void ShowInternal(UIView view, bool clearStack)
    {
        if (view == null)
            return;

        // 현재 스택에 있는 것들 숨기기
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
