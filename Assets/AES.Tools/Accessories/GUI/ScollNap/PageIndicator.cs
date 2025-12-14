using System;
using System.Collections.Generic;
using UnityEngine;


namespace AES.Tools
{
    public class PageIndicator : MonoBehaviour, IPageIndicator
    {
        public event Action<int> OnPageClicked;

        [SerializeField, AesReadOnly]
        private List<BottomNavItem> items = new();

        [Header("Options")]
        [SerializeField] private bool autoCollect = true;
        [SerializeField] private bool updateSelectionImmediatelyOnClick = false;

        private bool _bound;
        private int _currentPage = -1;

        // Unbind를 위해 핸들러를 캐시
        private readonly Dictionary<BottomNavItem, Action<int>> _handlers = new();

        private void Awake()
        {
            if (autoCollect)
                Collect();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!autoCollect) return;
            Collect();
        }
#endif

        private void OnDestroy()
        {
            Unbind();
        }

        private void Collect()
        {
            items.Clear();

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var item = child.GetComponent<BottomNavItem>();
                if (item == null)
                {
                    Debug.LogError($"[PageIndicator] BottomNavItem missing on {child.name}", child);
                    continue;
                }

                // 씬 배치(자식) 순서 = 인덱스
                item.SetPageIndex(i);

                items.Add(item);
            }
        }

        public void SetPageCount(int count)
        {
            if (_bound) return;
            _bound = true;

            if (autoCollect)
                Collect();

            if (items.Count != count)
                Debug.LogWarning($"[PageIndicator] ItemCount({items.Count}) != PageCount({count})", this);

            Bind();
        }

        private void Bind()
        {
            Unbind();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;

                // BottomNavItem.Clicked(Action<int>) 시그니처에 맞는 핸들러 생성
                Action<int> handler = (pageIndex) =>
                {
                    // pageIndex는 item 내부에서 Invoke(pageIndex)로 보내주는 값
                    if (updateSelectionImmediatelyOnClick)
                        SetCurrentPage(pageIndex);

                    OnPageClicked?.Invoke(pageIndex);
                };

                _handlers[item] = handler;
                item.Clicked += handler;
            }
        }

        private void Unbind()
        {
            if (items == null) return;

            foreach (var kv in _handlers)
            {
                if (kv.Key != null)
                    kv.Key.Clicked -= kv.Value;
            }
            _handlers.Clear();
        }

        public void SetCurrentPage(int page)
        {
            if (_currentPage == page) return;
            _currentPage = page;

            for (int i = 0; i < items.Count; i++)
                items[i]?.SetSelected(i == page);
        }

        // 런타임에 탭 구성 바뀌면 호출
        public void Rebuild(int pageCount)
        {
            _bound = false;
            SetPageCount(pageCount);
            SetCurrentPage(Mathf.Clamp(_currentPage, 0, items.Count - 1));
        }
    }
}
