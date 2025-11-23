using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools.Bindings
{
    public interface IVirtualizedItemBinder
    {
        void Bind(object data, int index);
    }

    [RequireComponent(typeof(ScrollRect))]
    public class VirtualizedListBinding : ContextBindingBase
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private float itemHeight = 40f;
        [SerializeField] private int extraBuffer = 2;

        private IObservableList _list;
        private ScrollRect _scrollRect;

        private readonly List<RectTransform> _items = new();
        private int _visibleCount;
        private int _currentStartIndex;

        protected override void Subscribe()
        {
            _scrollRect = GetComponent<ScrollRect>();

            _list = ResolveObservableList();
            if (_list == null || content == null || itemPrefab == null || _scrollRect == null)
                return;

            _list.OnListChanged += RefreshAll;

            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            RefreshAll();
        }

        protected override void Unsubscribe()
        {
            if (_list != null)
                _list.OnListChanged -= RefreshAll;

            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);

            ClearItems();
        }

        private void RefreshAll()
        {
            ClearItems();
            if (_list == null) return;

            // content height 설정
            var height = _list.Count * itemHeight;
            content.sizeDelta = new Vector2(content.sizeDelta.x+10, height);

            // 화면에 필요한 아이템 개수 계산
            var viewportHeight = (_scrollRect.viewport != null
                ? _scrollRect.viewport.rect.height
                : ((RectTransform)_scrollRect.transform).rect.height);

            _visibleCount = Mathf.CeilToInt(viewportHeight / itemHeight) + extraBuffer;

            for (int i = 0; i < _visibleCount; i++)
            {
                var go = Object.Instantiate(itemPrefab, content);
                var rt = go.GetComponent<RectTransform>();
                _items.Add(rt);
            }

            _currentStartIndex = 0;
            UpdateVisibleItems(force: true);
        }

        private void ClearItems()
        {
            foreach (var rt in _items)
            {
                if (rt != null)
                    Object.Destroy(rt.gameObject);
            }

            _items.Clear();
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            UpdateVisibleItems(force: false);
        }

        private void UpdateVisibleItems(bool force)
        {
            if (_list == null || _items.Count == 0) return;

            // content의 상단 기준 현재 스크롤 offset
            var contentTop = content.anchoredPosition.y;
            var newStartIndex = Mathf.FloorToInt(contentTop / itemHeight);
            newStartIndex = Mathf.Max(0, newStartIndex);
            if (newStartIndex == _currentStartIndex && !force)
                return;

            _currentStartIndex = newStartIndex;

            for (int i = 0; i < _items.Count; i++)
            {
                var index = _currentStartIndex + i;
                var rt = _items[i];

                if (index >= _list.Count)
                {
                    rt.gameObject.SetActive(false);
                    continue;
                }

                rt.gameObject.SetActive(true);

                // 위치 배치
                float y = -index * itemHeight - itemHeight * 0.5f;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y);

                // 데이터 바인딩
                var binder = rt.GetComponent<IVirtualizedItemBinder>();
                if (binder != null)
                    binder.Bind(_list.GetItem(index), index);
            }
        }
    }
}