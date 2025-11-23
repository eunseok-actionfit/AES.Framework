using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools.Bindings
{

    [RequireComponent(typeof(ScrollRect))]
    public class VirtualizedListBinding : ContextBindingBase
    {
        [Header("Core")]
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject itemPrefab;
        
        
        [Tooltip("뷰포트에 필요한 수 + 여유 버퍼")]
        [SerializeField] private int extraBuffer = 2;
        
        private float itemHeight = 0f;

        [Header("Selection")]
        [Tooltip("선택된 인덱스를 보관하는 IObservableProperty<int> 경로 (예: SelectedIndex)")]
        [SerializeField] private string selectedIndexPath;

        [Tooltip("SelectedIndex 변경 시 해당 인덱스로 자동 스크롤할지 여부")]
        [SerializeField] private bool autoScrollToSelectedIndex = true;

        private IObservableList _list;
        private ScrollRect _scrollRect;

        // Pool / Spacer
        private readonly List<RectTransform> _pool = new();
        private int _poolSize;
        private LayoutElement _topSpacer;
        private LayoutElement _bottomSpacer;

        private int _currentStartIndex;

        // Selection
        private IObservableProperty _selectedIndexProp;
        private int _selectedIndex = -1;
        
        
        void OnValidate()
        {
            _scrollRect ??= GetComponent<ScrollRect>();
            if (_scrollRect != null && content == null)
                content = _scrollRect.content;
        }

        protected override void Subscribe()
        {
            if (itemPrefab == null)
            {
                Debug.LogError("[VirtualizedListBinding] itemPrefab 이 없습니다.", this);
                return;
            }

            InitItemHeightFromPrefab();
            EnsureSpacers();

            _list = ResolveObservableList();
            if (_list == null)
                return;

            _list.OnListChanged += OnListChanged;
            _scrollRect.onValueChanged.AddListener(OnScrollChanged);

            ResolveSelectedIndexBinding();
            RebuildPoolAndRefresh();
        }

        protected override void Unsubscribe()
        {
            if (_list != null)
                _list.OnListChanged -= OnListChanged;

            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);

            if (_selectedIndexProp != null)
            {
                _selectedIndexProp.OnValueChangedBoxed -= OnSelectedIndexChanged;
                _selectedIndexProp = null;
            }

            ClearPool();
        }

        // ============
        // INIT
        // ============

        private void InitItemHeightFromPrefab()
        {
            // 임시 오브젝트 생성 → 레이아웃 강제 계산 → 높이 추출
            var temp = Instantiate(itemPrefab, content);
            var rt = temp.GetComponent<RectTransform>();

            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            itemHeight = rt.rect.height;
            if (itemHeight <= 0.01f)
                itemHeight = 40f; // 비상용

            Destroy(temp);
        }

        private void EnsureSpacers()
        {
            if (_topSpacer == null)
            {
                var go = new GameObject("TopSpacer", typeof(RectTransform));
                go.transform.SetParent(content, false);
                _topSpacer = go.AddComponent<LayoutElement>();
                _topSpacer.preferredHeight = 0;
            }

            if (_bottomSpacer == null)
            {
                var go = new GameObject("BottomSpacer", typeof(RectTransform));
                go.transform.SetParent(content, false);
                _bottomSpacer = go.AddComponent<LayoutElement>();
                _bottomSpacer.preferredHeight = 0;
            }
        }

        // ============
        // SELECTION
        // ============

        private void ResolveSelectedIndexBinding()
        {
            if (string.IsNullOrEmpty(selectedIndexPath))
                return;

            var ctx = Context;
            if (ctx == null || ctx.ViewModel == null)
                return;

            var path = MemberPathCache.Get(ctx.ViewModel.GetType(), selectedIndexPath);
            var value = path.GetValue(ctx.ViewModel);

            if (value is IObservableProperty prop)
            {
                _selectedIndexProp = prop;
                _selectedIndexProp.OnValueChangedBoxed += OnSelectedIndexChanged;
                _selectedIndex = ConvertToInt(_selectedIndexProp.Value);
            }
        }

        private void OnSelectedIndexChanged(object val)
        {
            _selectedIndex = ConvertToInt(val);

            if (autoScrollToSelectedIndex && _selectedIndex >= 0)
                ScrollToIndex(_selectedIndex, false);

            RefreshSelectionStates();
        }

        private int ConvertToInt(object v)
        {
            if (v is int i) return i;
            if (v == null) return -1;
            if (int.TryParse(v.ToString(), out var r)) return r;
            return -1;
        }

        // ============
        // LIST CHANGES
        // ============

        private void OnListChanged()
        {
            RebuildPoolAndRefresh();
        }

        private void OnScrollChanged(Vector2 _)
        {
            UpdateVisibleItems(false);
        }

        // ============
        // POOL
        // ============

        private void RebuildPoolAndRefresh()
        {
            ClearPool();
            if (_list == null)
                return;

            float viewportHeight = (_scrollRect.viewport != null
                ? _scrollRect.viewport.rect.height
                : ((RectTransform)_scrollRect.transform).rect.height);

            _poolSize = Mathf.CeilToInt(viewportHeight / itemHeight) + extraBuffer;
            _poolSize = Mathf.Max(1, _poolSize);

            // create pool between top/bottom spacer
            int insertIndex = _topSpacer.transform.GetSiblingIndex() + 1;
            for (int i = 0; i < _poolSize; i++)
            {
                var go = Instantiate(itemPrefab, content);
                go.transform.SetSiblingIndex(insertIndex + i);

                var rt = go.GetComponent<RectTransform>();
                _pool.Add(rt);
            }

            // bottom spacer at last
            _bottomSpacer.transform.SetAsLastSibling();

            _currentStartIndex = 0;
            UpdateVisibleItems(true);
        }

        private void ClearPool()
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

        private void UpdateVisibleItems(bool force)
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

        private void RefreshSelectionStates()
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
