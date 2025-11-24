using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class UINavigation : MonoBehaviour
{
    [System.Serializable]
    public class ViewEntry
    {
        public string key;
        public UIView view;
    }

   // [SerializeField] private List<ViewEntry> views = new List<ViewEntry>();

    private readonly Stack<UIView> _stack = new Stack<UIView>();
    private Dictionary<string, UIView> _viewMap;

    public UIView CurrentView => _stack.Count > 0 ? _stack.Peek() : null;

    private void Awake()
    {
        // 하위에서 자동 수집도 가능
        if (views == null || views.Count == 0)
        {
            var uiViews = GetComponentsInChildren<UIView>(true);
            views = uiViews
                .Select(v => new ViewEntry { key = v.name, view = v })
                .ToList();
        }

        _viewMap = views
            .Where(e => e.view != null && !string.IsNullOrEmpty(e.key))
            .ToDictionary(e => e.key, e => e.view);

        // 시작 시 전부 숨기고 필요하면 초기 화면을 Push
        foreach (var entry in _viewMap.Values)
        {
            entry.Hide();
        }
    }

    public void Push(string key)
    {
        if (!_viewMap.TryGetValue(key, out var nextView) || nextView == null)
        {
            UnityEngine.Debug.LogWarning($"UINavigation: View with key {key} not found.");
            return;
        }

        // 현재 뷰 Hide
        if (CurrentView != null)
            CurrentView.Hide();

        _stack.Push(nextView);
        nextView.Show();
    }

    public void Pop()
    {
        if (_stack.Count == 0)
            return;

        var top = _stack.Pop();
        top.Hide();

        // 이전 뷰 다시 Show
        if (_stack.Count > 0)
        {
            var prev = _stack.Peek();
            prev.Show();
        }
    }

    public void PopTo(string key)
    {
        if (_stack.Count == 0)
            return;

        // key가 나올 때까지 pop
        UIView target = null;
        var temp = new Stack<UIView>();

        while (_stack.Count > 0)
        {
            var v = _stack.Pop();
            v.Hide();
            temp.Push(v);

            if (_viewMap.TryGetValue(key, out var view) && v == view)
            {
                target = v;
                break;
            }
        }

        // 목표 못 찾았으면 복구X, 현재는 빈 상태
        if (target == null)
        {
            UnityEngine.Debug.LogWarning($"UINavigation: PopTo failed, key {key} not in stack.");
            return;
        }

        // target만 다시 스택에
        _stack.Clear();
        _stack.Push(target);
        target.Show();
    }

    public void PopToRoot()
    {
        if (_stack.Count == 0)
            return;

        // 전부 Hide
        while (_stack.Count > 1)
        {
            var v = _stack.Pop();
            v.Hide();
        }

        // 루트만 다시 Show
        var root = _stack.Peek();
        root.Show();
    }

    /// <summary>
    /// 이 Navigation 자체를 숨길 때 (Controller에서 호출)
    /// </summary>
    public void HideAll()
    {
        foreach (var v in _viewMap.Values)
        {
            v.Hide();
        }
    }
}
