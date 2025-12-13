using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools
{
    [RequireComponent(typeof(ScrollRect))]
    public class VirtualizedListBinding : ContextBindingBase
    {
        [Header("Core")]
        [SerializeField] RectTransform content;
        [SerializeField] UnityEngine.GameObject itemPrefab;

        [Tooltip("뷰포트에 필요한 수 + 여유 버퍼")]
        [SerializeField] int extraBuffer = 2;

        float itemHeight = 0f;

        [Header("Selection")]
        [Tooltip("선택된 인덱스를 보관하는 경로 (예: SelectedIndex)")]
        [SerializeField] string selectedIndexPath;

        [Tooltip("SelectedIndex 변경 시 해당 인덱스로 자동 스크롤할지 여부")]
        [SerializeField] bool autoScrollToSelectedIndex = true;

        IObservableList _list;
        ScrollRect _scrollRect;

        readonly List<RectTransform> _pool = new();
        int _poolSize;
        LayoutElement _topSpacer;
        LayoutElement _bottomSpacer;

        int _currentStartIndex;

        int _selectedIndex = -1;

        IBindingContext _ctx;
        object _listListenerToken;
        object _selectedIndexListenerToken;

        void OnValidate()
        {
            _scrollRect ??= GetComponent<ScrollRect>();
            if (_scrollRect != null && content == null)
                content = _scrollRect.content;
        }

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            if (itemPrefab == null)
            {
                Debug.LogError("[VirtualizedListBinding] itemPrefab 이 없습니다.", this);
                return;
            }

            _ctx = context;

            _scrollRect ??= GetComponent<ScrollRect>();
            if (_scrollRect == null)
            {
                Debug.LogError("[VirtualizedListBinding] ScrollRect 가 필요합니다.", this);
                return;
            }

            if (content == null)
                content = _scrollRect.content;

            InitItemHeightFromPrefab();
            EnsureSpacers();

            _listListenerToken = context.RegisterListener(path, OnListValueChanged);
            _scrollRect.onValueChanged.AddListener(OnScrollChanged);

            ResolveSelectedIndexBinding();
        }

        protected override void OnContextUnavailable()
        {
            if (_ctx != null && _listListenerToken != null)
            {
                _ctx.RemoveListener(ResolvedPath, _listListenerToken);
            }

            if (_ctx != null && !string.IsNullOrEmpty(selectedIndexPath) && _selectedIndexListenerToken != null)
            {
                _ctx.RemoveListener(selectedIndexPath, _selectedIndexListenerToken);
            }

            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);

            ClearPool();
            _list = null;
            _ctx = null;
            _listListenerToken = null;
            _selectedIndexListenerToken = null;
        }

        // ============
        // INIT
        // ============

        void InitItemHeightFromPrefab()
        {
            var temp = Instantiate(itemPrefab, content);
            var rt = temp.GetComponent<RectTransform>();

            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            itemHeight = rt.rect.height;
            if (itemHeight <= 0.01f)
                itemHeight = 40f;

            Destroy(temp);
        }

        void EnsureSpacers()
        {
            if (_topSpacer == null)
            {
                var go = new UnityEngine.GameObject("TopSpacer", typeof(RectTransform));
                go.transform.SetParent(content, false);
                _topSpacer = go.AddComponent<LayoutElement>();
                _topSpacer.preferredHeight = 0;
            }

            if (_bottomSpacer == null)
            {
                var go = new UnityEngine.GameObject("BottomSpacer", typeof(RectTransform));
                go.transform.SetParent(content, false);
                _bottomSpacer = go.AddComponent<LayoutElement>();
                _bottomSpacer.preferredHeight = 0;
            }
        }

        // ============
        // SELECTION
        // ============

        void ResolveSelectedIndexBinding()
        {
            if (string.IsNullOrEmpty(selectedIndexPath) || _ctx == null)
                return;

            _selectedIndexListenerToken =
                _ctx.RegisterListener(selectedIndexPath, OnSelectedIndexChanged);
        }

        void OnSelectedIndexChanged(object val)
        {
            _selectedIndex = ConvertToInt(val);

            if (autoScrollToSelectedIndex && _selectedIndex >= 0)
                ScrollToIndex(_selectedIndex, false);

            RefreshSelectionStates();
        }

        int ConvertToInt(object v)
        {
            if (v is int i) return i;
            if (v == null) return -1;
            if (int.TryParse(v.ToString(), out var r)) return r;
            return -1;
        }

        // ============
        // LIST CHANGES
        // ============

        void OnListValueChanged(object value)
        {
            _list = value as IObservableList;

            if (_list == null)
            {
                ClearPool();
                return;
            }

            RebuildPoolAndRefresh();
        }

        void OnScrollChanged(Vector2 _)
        {
            UpdateVisibleItems(false);
        }

        // ============
        // POOL
        // ============

        void RebuildPoolAndRefresh()
        {
            ClearPool();
            if (_list == null)
                return;

            float viewportHeight = (_scrollRect.viewport != null
                ? _scrollRect.viewport.rect.height
                : ((RectTransform)_scrollRect.transform).rect.height);

            _poolSize = Mathf.CeilToInt(viewportHeight / itemHeight) + extraBuffer;
            _poolSize = Mathf.Max(1, _poolSize);

            int insertIndex = _topSpacer.transform.GetSiblingIndex() + 1;
            for (int i = 0; i < _poolSize; i++)
            {
                var go = Instantiate(itemPrefab, content);
                go.transform.SetSiblingIndex(insertIndex + i);

                var rt = go.GetComponent<RectTransform>();
                _pool.Add(rt);
            }

            _bottomSpacer.transform.SetAsLastSibling();

            _currentStartIndex = 0;
            UpdateVisibleItems(true);
        }

        void ClearPool()
        {
            foreach (var rt in _pool)
            {
                if (rt != null)
                    Destroy(rt.gameObject);
            }
            _pool.Clear();
        }

        // ============
        // VISIBILITY
        // ============

        public void ScrollToIndex(int index, bool alignToTop = true)
        {
            if (_list == null || _list.Count == 0)
                return;

            index = Mathf.Clamp(index, 0, _list.Count - 1);

            float totalHeight = itemHeight * _list.Count;
            float viewportHeight = _scrollRect.viewport.rect.height;

            float targetY;

            if (alignToTop)
            {
                targetY = index * itemHeight;
            }
            else
            {
                float centerOffset = (viewportHeight - itemHeight) * 0.5f;
                targetY = index * itemHeight - centerOffset;
            }

            targetY = Mathf.Clamp(targetY, 0, Mathf.Max(0, totalHeight - viewportHeight));

            var pos = content.anchoredPosition;
            pos.y = targetY;
            content.anchoredPosition = pos;

            UpdateVisibleItems(true);
        }

        void UpdateVisibleItems(bool force)
        {
            if (_list == null)
                return;

            float contentPosY = content.anchoredPosition.y;
            int newStart = Mathf.FloorToInt(contentPosY / itemHeight);
            newStart = Mathf.Clamp(newStart, 0, Mathf.Max(0, _list.Count - 1));

            if (!force && newStart == _currentStartIndex)
                return;

            _currentStartIndex = newStart;

            int endIndex = Mathf.Min(_list.Count, _currentStartIndex + _poolSize);

            float topHeight = _currentStartIndex * itemHeight;
            float bottomHeight = Mathf.Max(0, (_list.Count - endIndex) * itemHeight);

            _topSpacer.preferredHeight = topHeight;
            _bottomSpacer.preferredHeight = bottomHeight;

            for (int i = 0; i < _poolSize; i++)
            {
                int dataIndex = _currentStartIndex + i;
                var rt = _pool[i];

                if (dataIndex < _list.Count)
                {
                    if (!rt.gameObject.activeSelf)
                        rt.gameObject.SetActive(true);

                    var binder = rt.GetComponent<IVirtualizedItemBinder>();
                    if (binder != null)
                    {
                        binder.Bind(_list.GetItem(dataIndex), dataIndex);

                        if (binder is ISelectableVirtualizedItemBinder selectable)
                            selectable.SetSelected(dataIndex == _selectedIndex);
                    }
                }
                else
                {
                    if (rt.gameObject.activeSelf)
                        rt.gameObject.SetActive(false);
                }
            }
        }

        void RefreshSelectionStates()
        {
            if (_list == null)
                return;

            for (int i = 0; i < _poolSize; i++)
            {
                int dataIndex = _currentStartIndex + i;
                if (dataIndex < 0 || dataIndex >= _list.Count)
                    continue;

                var selectable = _pool[i].GetComponent<ISelectableVirtualizedItemBinder>();
                if (selectable != null)
                    selectable.SetSelected(dataIndex == _selectedIndex);
            }
        }
    }
}
