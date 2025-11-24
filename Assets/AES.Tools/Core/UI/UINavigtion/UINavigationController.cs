using System.Collections.Generic;
using UnityEngine;

public class UINavigationController : MonoBehaviour
{
    [System.Serializable]
    public class NavEntry
    {
        public string key;
        public UINavigation navigation;
    }

    [SerializeField] private List<NavEntry> navigations = new List<NavEntry>();

    private Dictionary<string, UINavigation> _navMap;
    private UINavigation _currentNav;

    private void Awake()
    {
        if (navigations == null || navigations.Count == 0)
        {
            var navs = GetComponentsInChildren<UINavigation>(true);
            navigations = new List<NavEntry>();
            foreach (var n in navs)
            {
                navigations.Add(new NavEntry
                {
                    key = n.name,
                    navigation = n
                });
            }
        }

        _navMap = new Dictionary<string, UINavigation>();
        foreach (var entry in navigations)
        {
            if (entry.navigation == null || string.IsNullOrEmpty(entry.key))
                continue;

            _navMap[entry.key] = entry.navigation;
        }

        // 시작 시 전부 비활성화
        foreach (var nav in _navMap.Values)
        {
            nav.gameObject.SetActive(false);
        }
    }

    public void SwitchTo(string key)
    {
        if (!_navMap.TryGetValue(key, out var nextNav) || nextNav == null)
        {
            Debug.LogWarning($"UINavigationController: Nav {key} not found.");
            return;
        }

        if (_currentNav == nextNav)
            return;

        // 기존 Nav 숨기기
        if (_currentNav != null)
        {
            _currentNav.HideAll();
            _currentNav.gameObject.SetActive(false);
        }

        // 새 Nav 활성화 (내부 stack 상태는 그대로 유지)
        _currentNav = nextNav;
        _currentNav.gameObject.SetActive(true);
    }

    public UINavigation CurrentNavigation => _currentNav;
}