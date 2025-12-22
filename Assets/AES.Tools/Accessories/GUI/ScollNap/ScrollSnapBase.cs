using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace AES.Tools
{
    /// <summary>
    /// 페이지 단위 스냅 이동을 제공하는 공통 베이스.<br/>
    /// 스와이프, 버튼, 페이지 이동 이벤트를 한곳에서 관리한다.
    /// </summary>
    /// <remarks>
    /// 가로/세로 방향은 파생 클래스에서 설정한다.<br/>
    /// 자식 배치와 스냅 보정은 파생 클래스 구현에 의존한다.
    /// </remarks>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollSnapBase : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollSnap, IPointerClickHandler
    {
        // 패널(뷰포트) Rect 정보 캐시
        internal Rect panelDimensions;

        // 페이지(자식) 컨테이너
        internal RectTransform _screensContainer;

        // 세로 스크롤 여부
        internal bool _isVertical;

        // 페이지 개수
        internal int _screens = 1;

        // 스크롤 시작 위치 기준값
        internal float _scrollStartPosition;

        // 페이지 1칸 이동 거리
        internal float _childSize;

        // 페이지 위치 계산용 임시 값
        private float _childPos;

        // 마스크 영역 크기 캐시
        private float _maskSize;

        // 자식 앵커/피벗 기준
        internal Vector2 _childAnchorPoint;

        // ScrollRect 캐시
        internal ScrollRect _scroll_rect;

        // Lerp 목표 위치
        internal Vector3 _lerp_target;

        // Lerp 진행 여부
        internal bool _lerp;

        // 드래그 중 여부
        internal bool _pointerDown = false;

        // 현재 페이지에 정착 여부
        internal bool _settled = true;

        // 드래그 시작 위치
        internal Vector3 _startPosition = new Vector3();

        // 현재 페이지 인덱스
        internal int _currentPage;

        // 이전 페이지 인덱스
        internal int _previousPage;

        // 마스크 내 표시 아이템 절반 개수
        internal int _halfNoVisibleItems;

        // 무한 스크롤 사용 여부
        internal bool _isInfinite;

        // 무한 스크롤 윈도우 인덱스
        internal int _infiniteWindow;

        // 무한 스크롤 오프셋
        internal float _infiniteOffset;

        // 마스크 가시 범위 계산용 인덱스
        private int _bottomItem, _topItem;

        // 시작 이벤트 중복 호출 방지
        internal bool _startEventCalled = false;

        // 종료 이벤트 중복 호출 방지
        internal bool _endEventCalled = false;

        // 이벤트 일시 중지 여부
        internal bool _suspendEvents = false;

        [Serializable]
        public class SelectionChangeStartEvent : UnityEvent { }

        [Serializable]
        public class SelectionPageChangedEvent : UnityEvent<int> { }

        [Serializable]
        public class SelectionChangeEndEvent : UnityEvent<int> { }

        [SerializeField]
        [Tooltip("시작 페이지 인덱스이다.\n0부터 시작한다.")]
        public int StartingScreen = 0;

        [SerializeField]
        [Range(0, 8)]
        [Tooltip("페이지 간격 배율이다.\n기본은 1로, 바로 다음 페이지가 이어진다.")]
        public float PageStep = 1;


        [Tooltip("페이지네이션 토글 컨테이너이다.\n없어도 동작한다.")]
        [SerializeField] private GameObject PageIndicator;


        [Tooltip("이전 페이지 버튼 오브젝트이다.\n없어도 동작한다.")]
        [SerializeField] private GameObject PrevControl;


        [Tooltip("다음 페이지 버튼 오브젝트이다.\n없어도 동작한다.")]
        [SerializeField] private GameObject NextControl;

        [Tooltip("페이지 전환 Lerp 속도이다.\n값이 클수록 빠르다.")]
        public float transitionSpeed = 7.5f;

        [Tooltip("강제 스와이프 모드이다.\n항상 이전/다음 페이지로만 이동한다.")]
        public bool UseHardSwipe = false;

        [Tooltip("빠른 스와이프를 사용한다.\n일정 거리 이상이면 페이지를 넘긴다.")]
        public bool UseFastSwipe = false;


        [Tooltip("입력 델타 임계값을 사용한다.\n작은 스와이프는 가장 가까운 페이지로 복귀한다.")]
        public bool UseSwipeDeltaThreshold = false;

        [Tooltip("빠른 스와이프 거리 임계값(픽셀)이다.")]
        public int FastSwipeThreshold = 100;

        [Tooltip("관성 속도 임계값이다.\n이 값보다 느리면 스냅을 시도한다.")]
        public int SwipeVelocityThreshold = 100;

        [Tooltip("스와이프 델타 임계값이다.\n이 값보다 작으면 페이지를 넘기지 않는다.")]
        public float SwipeDeltaThreshold = 5.0f;

        [Tooltip("시간 스케일을 적용한다.\n꺼두면 UnscaledDeltaTime을 사용한다.")]
        public bool UseTimeScale = true;

        [Tooltip("가시 영역 마스크이다.\nRectMask2D 사용을 권장한다.")]
        public RectTransform MaskArea;

        [Tooltip("마스크 버퍼 배율이다.\n값이 작으면 더 자주 껐다 켠다.")]
        public float MaskBuffer = 1;

        /// <summary>
        /// 현재 페이지 인덱스를 반환한다.<br/>
        /// 값 변경 시 페이지 변경 이벤트를 갱신한다.
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            internal set
            {
                if (_isInfinite)
                {
                    float infiniteWindow = value / (float)_screensContainer.childCount;

                    if (infiniteWindow < 0)
                        _infiniteWindow = (int)System.Math.Floor(infiniteWindow);
                    else
                        _infiniteWindow = value / _screensContainer.childCount;

                    _infiniteWindow = value < 0 ? (-_infiniteWindow) : _infiniteWindow;

                    value = value % _screensContainer.childCount;

                    if (value < 0)
                        value = _screensContainer.childCount + value;
                    else if (value > _screensContainer.childCount - 1)
                        value = value - _screensContainer.childCount;
                }

                if ((value != _currentPage && value >= 0 && value < _screensContainer.childCount) ||
                    (value == 0 && _screensContainer.childCount == 0))
                {
                    _previousPage = _currentPage;
                    _currentPage = value;

                    if (MaskArea)
                        UpdateVisible();

                    if (!_lerp)
                        ScreenChange();

                    OnCurrentScreenChange(_currentPage);
                }
            }
        }

        [Tooltip("활성화 시 위치를 즉시 점프한다.\n기본은 Lerp로 시작 위치로 이동한다.")]
        public bool JumpOnEnable = false;

        [Tooltip("활성화 시 시작 페이지로 되돌린다.\n꺼두면 현재 선택을 유지한다.")]
        public bool RestartOnEnable = false;


        [Tooltip("자식 초기화 시 부모 Transform 값을 사용한다.\n특수 연출이 필요하면 끌 수 있다.")]
        public bool UseParentTransform = true;


        [Tooltip("자식 페이지 프리팹 배열이다.\n씬 자식과 혼용하지 않는다.")]
        public GameObject[] ChildObjects;


        [SerializeField]
        [Tooltip("선택 변경이 시작될 때 호출된다.")]
        private SelectionChangeStartEvent m_OnSelectionChangeStartEvent = new SelectionChangeStartEvent();

        /// <summary>
        /// 선택 변경 시작 이벤트를 제공한다.
        /// </summary>
        public SelectionChangeStartEvent OnSelectionChangeStartEvent
        {
            get => m_OnSelectionChangeStartEvent;
            set => m_OnSelectionChangeStartEvent = value;
        }

        [SerializeField]
        [Tooltip("페이지가 바뀌는 동안 호출된다.\n드래그 중에도 호출될 수 있다.")]
        private SelectionPageChangedEvent m_OnSelectionPageChangedEvent = new SelectionPageChangedEvent();

        /// <summary>
        /// 페이지 변경 이벤트를 제공한다.
        /// </summary>
        public SelectionPageChangedEvent OnSelectionPageChangedEvent
        {
            get => m_OnSelectionPageChangedEvent;
            set => m_OnSelectionPageChangedEvent = value;
        }

        [SerializeField]
        [Tooltip("페이지가 정착했을 때 호출된다.")]
        private SelectionChangeEndEvent m_OnSelectionChangeEndEvent = new SelectionChangeEndEvent();

        /// <summary>
        /// 선택 변경 종료 이벤트를 제공한다.
        /// </summary>
        public SelectionChangeEndEvent OnSelectionChangeEndEvent
        {
            get => m_OnSelectionChangeEndEvent;
            set => m_OnSelectionChangeEndEvent = value;
        }

        private IPageIndicator _indicator;
        private IPressable _prev;
        private IPressable _next;

        /// <summary>
        /// 필수 컴포넌트와 초기 상태를 구성한다.<br/>
        /// 버튼 바인딩과 자식 초기화를 수행한다.
        /// </summary>
        void Awake()
        {
            if (_scroll_rect == null) _scroll_rect = gameObject.GetComponent<ScrollRect>();

            panelDimensions = gameObject.GetComponent<RectTransform>().rect;
            if (StartingScreen < 0) StartingScreen = 0;

            _screensContainer = _scroll_rect.content;

            InitialiseChildObjects();


            // --- NEW: indicator 초기화 + 클릭 -> GoToScreen ---
            if (PageIndicator != null && PageIndicator.TryGetComponent(out _indicator))
            {
                _indicator.SetPageCount(_screensContainer.childCount);
                _indicator.SetCurrentPage(_currentPage);
                _indicator.OnPageClicked += OnIndicatorClicked;
            }

            // --- NEW: prev/next 클릭 바인딩 ---
            if (PrevControl != null && PrevControl.TryGetComponent(out _prev))
                _prev.Clicked += PreviousScreen;

            if (NextControl != null && NextControl.TryGetComponent(out _next))
                _next.Clicked += NextScreen;

            _isInfinite = GetComponent<UI_InfiniteScroll>() != null;
        }

        private void OnDestroy()
        {
            if (_indicator != null) _indicator.OnPageClicked -= OnIndicatorClicked;
            if (_prev != null) _prev.Clicked -= PreviousScreen;
            if (_next != null) _next.Clicked -= NextScreen;
        }

        private void OnIndicatorClicked(int page)
        {
            GoToScreen(page, true);
        }


        /// <summary>
        /// 자식 페이지 소스를 초기화한다.<br/>
        /// 배열 프리팹 또는 씬 자식 중 하나만 사용한다.
        /// </summary>
        internal void InitialiseChildObjects()
        {
            if (ChildObjects != null && ChildObjects.Length > 0)
            {
                if (_screensContainer.transform.childCount > 0)
                {
                    Debug.LogError("ScrollRect Content has children, this is not supported when using managed Child Objects\n Either remove the ScrollRect Content children or clear the ChildObjects array");
                    return;
                }

                InitialiseChildObjectsFromArray();

                if (GetComponent<UI_InfiniteScroll>() != null)
                    GetComponent<UI_InfiniteScroll>().Init();
            }
            else { InitialiseChildObjectsFromScene(); }
        }

        /// <summary>
        /// 씬에 배치된 자식을 기반으로 자식 배열을 구성한다.
        /// </summary>
        internal void InitialiseChildObjectsFromScene()
        {
            int childCount = _screensContainer.childCount;
            ChildObjects = new GameObject[childCount];

            for (int i = 0; i < childCount; i++)
            {
                ChildObjects[i] = _screensContainer.transform.GetChild(i).gameObject;

                if (MaskArea && ChildObjects[i].activeSelf)
                    ChildObjects[i].SetActive(false);
            }
        }

        /// <summary>
        /// 프리팹 배열을 인스턴스화하여 자식으로 구성한다.
        /// </summary>
        internal void InitialiseChildObjectsFromArray()
        {
            int childCount = ChildObjects.Length;

            for (int i = 0; i < childCount; i++)
            {
                var child = Instantiate(ChildObjects[i], _screensContainer.transform, true);

                if (UseParentTransform)
                {
                    var childRect = child.GetComponent<RectTransform>();
                    childRect.rotation = _screensContainer.rotation;
                    childRect.localScale = _screensContainer.localScale;
                    childRect.position = _screensContainer.position;
                }

                ChildObjects[i] = child;

                if (MaskArea && ChildObjects[i].activeSelf)
                    ChildObjects[i].SetActive(false);
            }
        }

        /// <summary>
        /// 마스크 기준으로 활성 페이지를 갱신한다.<br/>
        /// 필요 없는 오브젝트를 비활성화한다.
        /// </summary>
        internal void UpdateVisible()
        {
            if (!MaskArea || ChildObjects == null || ChildObjects.Length < 1 || _screensContainer.childCount < 1)
                return;

            _maskSize = _isVertical ? MaskArea.rect.height : MaskArea.rect.width;
            _halfNoVisibleItems = (int)System.Math.Round(_maskSize / (_childSize * MaskBuffer), MidpointRounding.AwayFromZero) / 2;
            _bottomItem = _topItem = 0;

            for (int i = _halfNoVisibleItems + 1; i > 0; i--)
            {
                _bottomItem = _currentPage - i < 0 ? 0 : i;
                if (_bottomItem > 0) break;
            }

            for (int i = _halfNoVisibleItems + 1; i > 0; i--)
            {
                _topItem = _screensContainer.childCount - _currentPage - i < 0 ? 0 : i;
                if (_topItem > 0) break;
            }

            for (int i = CurrentPage - _bottomItem; i < CurrentPage + _topItem; i++)
            {
                try { ChildObjects[i].SetActive(true); }
                catch { Debug.Log("Failed to setactive child [" + i + "]"); }
            }

            if (_currentPage > _halfNoVisibleItems)
                ChildObjects[CurrentPage - _bottomItem].SetActive(false);

            if (_screensContainer.childCount - _currentPage > _topItem)
                ChildObjects[CurrentPage + _topItem].SetActive(false);
        }

        /// <summary>
        /// 다음 페이지로 이동한다.
        /// </summary>
        public void NextScreen()
        {
            if (_currentPage < _screens - 1 || _isInfinite)
            {
                if (!_lerp)
                    StartScreenChange();

                _lerp = true;

                if (_isInfinite)
                    CurrentPage = GetPageforPosition(_screensContainer.anchoredPosition) + 1;
                else
                    CurrentPage = _currentPage + 1;

                GetPositionforPage(_currentPage, ref _lerp_target);
                ScreenChange();
            }
        }

        /// <summary>
        /// 이전 페이지로 이동한다.
        /// </summary>
        public void PreviousScreen()
        {
            if (_currentPage > 0 || _isInfinite)
            {
                if (!_lerp)
                    StartScreenChange();

                _lerp = true;

                if (_isInfinite)
                    CurrentPage = GetPageforPosition(_screensContainer.anchoredPosition) - 1;
                else
                    CurrentPage = _currentPage - 1;

                GetPositionforPage(_currentPage, ref _lerp_target);
                ScreenChange();
            }
        }

        /// <summary>
        /// 지정한 페이지로 이동한다.<br/>
        /// 인덱스는 0부터 시작한다.
        /// </summary>
        /// <param name="screenIndex">이동할 페이지 인덱스.</param>
        /// <param name="pagination">페이지네이션에서 호출된 이동인지 여부.</param>
        public void GoToScreen(int screenIndex, bool pagination = false)
        {
            if (screenIndex <= _screens - 1 && screenIndex >= 0)
            {
                if (!_lerp || pagination)
                    StartScreenChange();

                _lerp = true;
                CurrentPage = screenIndex;
                GetPositionforPage(_currentPage, ref _lerp_target);
                ScreenChange();
            }
        }

        /// <summary>
        /// 현재 위치에서 가장 가까운 페이지 인덱스를 계산한다.
        /// </summary>
        /// <param name="pos">컨테이너 로컬 위치.</param>
        /// <returns>가장 가까운 페이지 인덱스.</returns>
        internal int GetPageforPosition(Vector3 pos)
        {
            return _isVertical
                ? (int)System.Math.Round((_scrollStartPosition - pos.y) / _childSize)
                : (int)System.Math.Round((_scrollStartPosition - pos.x) / _childSize);
        }

        /// <summary>
        /// 현재 위치가 페이지 경계에 정착했는지 확인한다.
        /// </summary>
        /// <param name="pos">컨테이너 로컬 위치.</param>
        /// <returns>정착 상태면 <c>true</c>.</returns>
        internal bool IsRectSettledOnaPage(Vector3 pos)
        {
            return _isVertical
                ? Mathf.Approximately(-((pos.y - _scrollStartPosition) / _childSize), -(int)System.Math.Round((pos.y - _scrollStartPosition) / _childSize))
                : Mathf.Approximately(-((pos.x - _scrollStartPosition) / _childSize), -(int)System.Math.Round((pos.x - _scrollStartPosition) / _childSize));
        }

        /// <summary>
        /// 지정한 페이지의 목표 로컬 위치를 계산한다.
        /// </summary>
        /// <param name="page">페이지 인덱스.</param>
        /// <param name="target">계산 결과 위치.</param>
        internal void GetPositionforPage(int page, ref Vector3 target)
        {
            _childPos = -_childSize * page;

            if (_isVertical)
            {
                _infiniteOffset = _screensContainer.anchoredPosition.y < 0 ? -_screensContainer.sizeDelta.y * _infiniteWindow : _screensContainer.sizeDelta.y * _infiniteWindow;
                _infiniteOffset = _infiniteOffset == 0 ? 0 : _infiniteOffset < 0 ? _infiniteOffset - _childSize * _infiniteWindow : _infiniteOffset + _childSize * _infiniteWindow;
                target.y = _childPos + _scrollStartPosition + _infiniteOffset;
            }
            else
            {
                _infiniteOffset = _screensContainer.anchoredPosition.x < 0 ? -_screensContainer.sizeDelta.x * _infiniteWindow : _screensContainer.sizeDelta.x * _infiniteWindow;
                _infiniteOffset = _infiniteOffset == 0 ? 0 : _infiniteOffset < 0 ? _infiniteOffset - _childSize * _infiniteWindow : _infiniteOffset + _childSize * _infiniteWindow;
                target.x = _childPos + _scrollStartPosition + _infiniteOffset;
            }
        }

        /// <summary>
        /// 가장 가까운 페이지로 스냅 이동을 예약한다.
        /// </summary>
        internal void ScrollToClosestElement()
        {
            _lerp = true;
            CurrentPage = GetPageforPosition(_screensContainer.anchoredPosition);
            GetPositionforPage(_currentPage, ref _lerp_target);
            OnCurrentScreenChange(_currentPage);
        }

        /// <summary>
        /// 페이지 변경에 따른 UI 상태를 갱신한다.
        /// </summary>
        /// <param name="currentScreen">현재 페이지 인덱스.</param>
        internal void OnCurrentScreenChange(int currentScreen)
        {
            _indicator?.SetCurrentPage(currentScreen);

            if (!_isInfinite)
            {
                _prev?.SetInteractable(currentScreen > 0);
                _next?.SetInteractable(currentScreen < _screensContainer.childCount - 1);
            }
        }

        /// <summary>
        /// 인스펙터 값 변경 시 기본 제약을 보정한다.
        /// </summary>
        private void OnValidate()
        {
            if (_scroll_rect == null)
                _scroll_rect = GetComponent<ScrollRect>();

            if (!_scroll_rect.horizontal && !_scroll_rect.vertical)
                Debug.LogError("ScrollRect has to have a direction, please select either Horizontal OR Vertical with the appropriate control.");

            if (_scroll_rect.horizontal && _scroll_rect.vertical)
                Debug.LogError("ScrollRect는 단방향이어야 합니다. ScrollRect에서는 수평 또는 수직 중 하나만 사용하고 둘 다 사용할 수는 없습니다.");

            var content = GetComponent<ScrollRect>().content;

            if (content != null)
            {
                int children = content.childCount;

                if (children != 0 || ChildObjects != null)
                {
                    int childCount = ChildObjects == null || ChildObjects.Length == 0 ? children : ChildObjects.Length;

                    if (StartingScreen > childCount - 1)
                        StartingScreen = childCount - 1;

                    if (StartingScreen < 0)
                        StartingScreen = 0;
                }
            }

            if (MaskBuffer <= 0)
                MaskBuffer = 1;

            if (PageStep < 0)
                PageStep = 0;

            if (PageStep > 8)
                PageStep = 9;

            var infiniteScroll = GetComponent<UI_InfiniteScroll>();

            if (ChildObjects != null && ChildObjects.Length > 0 && infiniteScroll != null && !infiniteScroll.InitByUser) { Debug.LogError($"[{gameObject.name}]When using procedural children with a ScrollSnap (Adding Prefab ChildObjects) and the Infinite Scroll component\nYou must set the 'InitByUser' option to true, to enable late initialising"); }
        }

        /// <summary>
        /// 선택 변경 시작 이벤트를 발생시킨다.<br/>
        /// 스와이프 또는 버튼 이동에서 호출된다.
        /// </summary>
        public void StartScreenChange()
        {
            if (_startEventCalled)
                return;

            _suspendEvents = true;

            _startEventCalled = true;
            _endEventCalled = false;
            OnSelectionChangeStartEvent.Invoke();
        }

        /// <summary>
        /// 페이지 변경 이벤트를 발생시킨다.<br/>
        /// 이동 중에도 호출될 수 있다.
        /// </summary>
        internal void ScreenChange()
        {
            OnSelectionPageChangedEvent.Invoke(_currentPage);
        }

        /// <summary>
        /// 선택 변경 종료 이벤트를 발생시킨다.<br/>
        /// 페이지 정착 시 1회 호출된다.
        /// </summary>
        internal void EndScreenChange()
        {
            if (_endEventCalled)
                return;

            _suspendEvents = false;

            _endEventCalled = true;
            _startEventCalled = false;
            _settled = true;
            OnSelectionChangeEndEvent.Invoke(_currentPage);
        }

        /// <summary>
        /// 현재 페이지의 Transform을 반환한다.
        /// </summary>
        /// <returns>현재 페이지 Transform.</returns>
        public Transform CurrentPageObject()
        {
            return _screensContainer.GetChild(CurrentPage);
        }

        /// <summary>
        /// 현재 페이지의 Transform을 out 파라미터로 반환한다.
        /// </summary>
        /// <param name="returnObject">현재 페이지 Transform.</param>
        public void CurrentPageObject(out Transform returnObject)
        {
            returnObject = _screensContainer.GetChild(CurrentPage);
        }

        /// <summary>
        /// 드래그 시작 시 내부 상태를 설정한다.
        /// </summary>
        /// <param name="eventData">드래그 이벤트 데이터.</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            _pointerDown = true;
            _settled = false;
            StartScreenChange();
            _startPosition = _screensContainer.anchoredPosition;
        }

        /// <summary>
        /// 드래그 중 Lerp를 중지한다.
        /// </summary>
        /// <param name="eventData">드래그 이벤트 데이터.</param>
        public void OnDrag(PointerEventData eventData)
        {
            _lerp = false;
        }

        /// <summary>
        /// 드래그 종료 처리를 수행한다.<br/>
        /// 파생 클래스가 실제 스냅 규칙을 구현한다.
        /// </summary>
        /// <param name="eventData">드래그 이벤트 데이터.</param>
        public virtual void OnEndDrag(PointerEventData eventData) { }

        /// <summary>
        /// 현재 페이지 값을 ScrollBarHelper 규격으로 제공한다.
        /// </summary>
        /// <returns>현재 페이지 인덱스.</returns>
        int IScrollSnap.CurrentPage()
        {
            return CurrentPage = GetPageforPosition(_screensContainer.anchoredPosition);
        }

        /// <summary>
        /// Lerp 활성 여부를 설정한다.
        /// </summary>
        /// <param name="value">활성 여부.</param>
        public void SetLerp(bool value)
        {
            _lerp = value;
        }

        /// <summary>
        /// 지정 페이지로 이동한다.
        /// </summary>
        /// <param name="page">페이지 인덱스.</param>
        public void ChangePage(int page)
        {
            GoToScreen(page);
        }

        /// <summary>
        /// 클릭 이벤트를 수신한다.<br/>
        /// 기본 구현은 입력만 소비한다.
        /// </summary>
        /// <param name="eventData">포인터 이벤트 데이터.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            var position = _screensContainer.anchoredPosition;
        }
    }
}