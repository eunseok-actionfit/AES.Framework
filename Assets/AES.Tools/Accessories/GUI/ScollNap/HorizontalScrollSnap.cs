using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace AES.Tools
{
    /// <summary>
    /// 가로 방향 페이지 스냅 스크롤을 제공하는 컴포넌트.<br/>
    /// `ScrollSnapBase`를 상속하여 수평 스와이프 기반 페이지 전환을 처리한다.
    /// </summary>
    /// <remarks>
    /// 각 자식 요소를 화면 크기에 맞게 배치한다.<br/>
    /// 드래그 종료 시 가장 가까운 페이지로 자동 정렬된다.
    /// </remarks>
    [RequireComponent(typeof(ScrollRect))]
    [AddComponentMenu("Layout/Extensions/Horizontal Scroll Snap")]
    public class HorizontalScrollSnap : ScrollSnapBase
    {
        [Range(0.05f, 0.95f)]
        public float HardSwipeMoveRatio = 0.3333333f;
        
        // EndDrag 중복 호출을 방지하기 위한 플래그
        private bool updated = true;

        /// <summary>
        /// 초기 설정을 수행한다.<br/>
        /// 수평 스크롤 기준 값과 시작 페이지를 설정한다.
        /// </summary>
        private void Start()
        {
            _isVertical = false;
            _childAnchorPoint = new Vector2(0, 0.5f);
            _currentPage = StartingScreen;
            panelDimensions = GetComponent<RectTransform>().rect;
            UpdateLayout();
        }

        /// <summary>
        /// 스크롤 상태를 매 프레임 갱신한다.<br/>
        /// 관성 종료 시 페이지 정렬을 처리한다.
        /// </summary>
        private void Update()
        {
            updated = false;

            if (!_lerp && (_scroll_rect.velocity == Vector2.zero && _scroll_rect.inertia))
            {
                if (!_settled && !_pointerDown)
                {
                    if (!IsRectSettledOnaPage(_screensContainer.anchoredPosition)) { ScrollToClosestElement(); }
                }

                return;
            }
            else if (_lerp)
            {
                _screensContainer.anchoredPosition =
                    Vector3.Lerp(
                        _screensContainer.anchoredPosition,
                        _lerp_target,
                        transitionSpeed * (UseTimeScale ? Time.deltaTime : Time.unscaledDeltaTime)
                    );

                if (Vector3.Distance(_screensContainer.anchoredPosition, _lerp_target) < 0.2f)
                {
                    _screensContainer.anchoredPosition = _lerp_target;
                    _lerp = false;
                    EndScreenChange();
                }
            }

            if (UseHardSwipe)
                return;

            CurrentPage = GetPageforPosition(_screensContainer.anchoredPosition);

            if (!_pointerDown)
            {
                if (_scroll_rect.velocity.x > 0.01f || _scroll_rect.velocity.x < -0.01f)
                {
                    if (IsRectMovingSlowerThanThreshold(0)) { ScrollToClosestElement(); }
                }
            }
        }

        /// <summary>
        /// 현재 스크롤 속도가 스와이프 임계값 이하인지 확인한다.
        /// </summary>
        /// <param name="startingSpeed">기준 속도 값.</param>
        /// <returns>임계값보다 느리면 <c>true</c>.</returns>
        private bool IsRectMovingSlowerThanThreshold(float startingSpeed)
        {
            return (_scroll_rect.velocity.x > startingSpeed && _scroll_rect.velocity.x < SwipeVelocityThreshold) ||
                   (_scroll_rect.velocity.x < startingSpeed && _scroll_rect.velocity.x > -SwipeVelocityThreshold);
        }

        /// <summary>
        /// 모든 자식 페이지를 수평으로 재배치한다.<br/>
        /// 각 페이지를 패널 크기에 맞게 정렬한다.
        /// </summary>
        public void DistributePages()
        {
            _screens = _screensContainer.childCount;
            _scroll_rect.horizontalNormalizedPosition = 0;

            float offset = 0;
            float dimension = 0;
            Rect panelRect = GetComponent<RectTransform>().rect;
            float currentXPosition = 0;

            _childSize = (int)panelRect.width * ((PageStep == 0) ? 3 : PageStep);

            for (int i = 0; i < _screensContainer.childCount; i++)
            {
                RectTransform child = _screensContainer.GetChild(i).GetComponent<RectTransform>();
                //Debug.Log($"[PagesOrder] i={i} name={_screensContainer.GetChild(i).name}");
                currentXPosition = offset + i * _childSize;
                child.sizeDelta = new Vector2(panelRect.width, panelRect.height);
                child.anchoredPosition = new Vector2(currentXPosition, 0f);
                child.anchorMin = child.anchorMax = child.pivot = _childAnchorPoint;
            }

            dimension = currentXPosition + offset * -1;
            _screensContainer.GetComponent<RectTransform>().offsetMax = new Vector2(dimension, 0f);
        }

        /// <summary>
        /// 새로운 페이지 오브젝트를 추가한다.<br/>
        /// 추가 후 전체 레이아웃을 갱신한다.
        /// </summary>
        /// <param name="gameObject">추가할 페이지 오브젝트.</param>
        public void AddChild(GameObject gameObject)
        {
            AddChild(gameObject, false);
        }

        /// <summary>
        /// 새로운 페이지 오브젝트를 추가한다.<br/>
        /// 월드 좌표 유지 여부를 설정할 수 있다.
        /// </summary>
        /// <param name="gameObject">추가할 페이지 오브젝트.</param>
        /// <param name="worldPositionStays">월드 좌표 유지 여부.</param>
        public void AddChild(GameObject gameObject, bool worldPositionStays)
        {
            try { _scroll_rect.horizontalNormalizedPosition = 0; }
            catch
            {
                // Unity 내부 오류 방어 처리
            }

            gameObject.transform.SetParent(_screensContainer, worldPositionStays);
            InitialiseChildObjectsFromScene();
            DistributePages();

            if (MaskArea) { UpdateVisible(); }

            SetScrollContainerPosition();
        }

        /// <summary>
        /// 지정한 인덱스의 페이지를 제거한다.
        /// </summary>
        /// <param name="index">제거할 페이지 인덱스.</param>
        /// <param name="childRemoved">제거된 페이지 오브젝트.</param>
        public void RemoveChild(int index, out GameObject childRemoved)
        {
            RemoveChild(index, false, out childRemoved);
        }

        /// <summary>
        /// 지정한 인덱스의 페이지를 제거한다.<br/>
        /// 월드 좌표 유지 여부를 설정할 수 있다.
        /// </summary>
        /// <param name="index">제거할 페이지 인덱스.</param>
        /// <param name="worldPositionStays">월드 좌표 유지 여부.</param>
        /// <param name="childRemoved">제거된 페이지 오브젝트.</param>
        public void RemoveChild(int index, bool worldPositionStays, out GameObject childRemoved)
        {
            childRemoved = null;

            if (index < 0 || index >= _screensContainer.childCount)
                return;

            try { _scroll_rect.horizontalNormalizedPosition = 0; }
            catch
            {
                // Unity 내부 오류 방어 처리
            }

            Transform child = _screensContainer.GetChild(index);
            child.SetParent(null, worldPositionStays);
            childRemoved = child.gameObject;

            InitialiseChildObjectsFromScene();
            DistributePages();

            if (MaskArea)
                UpdateVisible();

            if (_currentPage > _screens - 1) { CurrentPage = _screens - 1; }

            SetScrollContainerPosition();
        }

        /// <summary>
        /// 모든 페이지를 제거한다.
        /// </summary>
        /// <param name="childrenRemoved">제거된 페이지 배열.</param>
        public void RemoveAllChildren(out GameObject[] childrenRemoved)
        {
            RemoveAllChildren(false, out childrenRemoved);
        }

        /// <summary>
        /// 모든 페이지를 제거한다.<br/>
        /// 월드 좌표 유지 여부를 설정할 수 있다.
        /// </summary>
        /// <param name="worldPositionStays">월드 좌표 유지 여부.</param>
        /// <param name="childrenRemoved">제거된 페이지 배열.</param>
        public void RemoveAllChildren(bool worldPositionStays, out GameObject[] childrenRemoved)
        {
            int count = _screensContainer.childCount;
            childrenRemoved = new GameObject[count];

            for (int i = count - 1; i >= 0; i--)
            {
                childrenRemoved[i] = _screensContainer.GetChild(i).gameObject;
                childrenRemoved[i].transform.SetParent(null, worldPositionStays);
            }

            _scroll_rect.horizontalNormalizedPosition = 0;
            CurrentPage = 0;

            InitialiseChildObjectsFromScene();
            DistributePages();

            if (MaskArea)
                UpdateVisible();
        }

        /// <summary>
        /// 현재 페이지 기준으로 스크롤 위치를 재설정한다.
        /// </summary>
        private void SetScrollContainerPosition()
        {
            _scrollStartPosition = _screensContainer.anchoredPosition.x;
            _scroll_rect.horizontalNormalizedPosition = (float)_currentPage / (_screens - 1);
            OnCurrentScreenChange(_currentPage);
        }

        /// <summary>
        /// 화면 해상도 변경 시 레이아웃을 갱신한다.
        /// </summary>
        /// <param name="resetPositionToStart">시작 페이지로 이동할지 여부.</param>
        public void UpdateLayout(bool resetPositionToStart = false)
        {
            _lerp = false;
            DistributePages();

            if (resetPositionToStart) { _currentPage = StartingScreen; }

            if (MaskArea) { UpdateVisible(); }

            SetScrollContainerPosition();
            OnCurrentScreenChange(_currentPage);
        }

        /// <summary>
        /// RectTransform 크기 변경 시 레이아웃을 갱신한다.
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            if (_childAnchorPoint != Vector2.zero) { UpdateLayout(); }
        }

        /// <summary>
        /// 컴포넌트 활성화 시 초기 상태를 재설정한다.
        /// </summary>
        private void OnEnable()
        {
            InitialiseChildObjectsFromScene();
            DistributePages();

            if (MaskArea)
                UpdateVisible();

            if (JumpOnEnable || !RestartOnEnable)
                SetScrollContainerPosition();

            if (RestartOnEnable)
                GoToScreen(StartingScreen);
        }

        /// <summary>
        /// 드래그 종료 시 페이지 스냅 처리를 수행한다.
        /// </summary>
        /// <param name="eventData">드래그 이벤트 데이터.</param>
        public override void OnEndDrag(PointerEventData eventData)
        {
            if (updated)
                return;

            updated = true;
            _pointerDown = false;

            if (!_scroll_rect.horizontal)
                return;

            if (UseSwipeDeltaThreshold && System.Math.Abs(eventData.delta.x) < SwipeDeltaThreshold)
            {
                ScrollToClosestElement();
                return;
            }

            float distance = Vector3.Distance(_startPosition, _screensContainer.anchoredPosition);

            if (UseHardSwipe)
            {
                _scroll_rect.velocity = Vector3.zero;

                float threshold = panelDimensions.width * HardSwipeMoveRatio; // 페이지 폭 기준
                if (distance > threshold)
                {
                    if (_startPosition.x - _screensContainer.anchoredPosition.x > 0)
                        NextScreen();
                    else
                        PreviousScreen();
                }
                else
                {
                    ScrollToClosestElement();
                }
            }
            else
            {
                if (UseFastSwipe && distance < panelDimensions.width && distance >= FastSwipeThreshold)
                {
                    _scroll_rect.velocity = Vector3.zero;

                    float dir = _startPosition.x - _screensContainer.anchoredPosition.x;
                    float absDir = Mathf.Abs(dir);

                    // 많이 움직였으면 넘기고, 애매하면 closest
                    if (absDir > _childSize / 3f)
                    {
                        if (dir > 0) NextScreen();
                        else PreviousScreen();
                    }
                    else
                    {
                        ScrollToClosestElement();
                    }
                    
                }
                else if (distance == 0) { EndScreenChange(); }
            }
        }
    }
}