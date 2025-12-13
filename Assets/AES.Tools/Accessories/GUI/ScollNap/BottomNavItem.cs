using UnityEngine;
using UnityEngine.Events;
using AES.Tools;
using AES.Tools.Controllers.Core;


public class BottomNavItem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TouchButton touchButton;

    [Header("Index (자동 주입 권장)")]
    [SerializeField] private int pageIndex = -1;

    [Header("Behavior")]
    [Tooltip("이미 선택된 탭을 다시 눌렀을 때 클릭을 무시합니다.")]
    [SerializeField] private bool blockReclickWhenSelected = true;

    [Tooltip("이미 선택된 탭을 다시 눌렀을 때 '새로고침/스크롤탑' 같은 동작을 실행합니다.")]
    [SerializeField] private bool allowRefreshOnReclick = false;

    [Header("Lock")]
    [Tooltip("잠금 상태면 탭 이동 대신 OnLockedClick 이벤트를 실행합니다.")]
    [SerializeField] private bool locked = false;

    [Header("Selection Events")]
    [SerializeField] private UnityEvent OnSelected;
    [SerializeField] private UnityEvent OnDeselected;

    [Header("Click Hooks")]
    [Tooltip("정상 클릭(이동 시도) 직전에 호출")]
    [SerializeField] private UnityEvent OnClick;

    [Tooltip("선택된 탭을 다시 눌렀을 때(Refresh용)")]
    [SerializeField] private UnityEvent OnReclickSelected;

    [Tooltip("잠긴 탭을 눌렀을 때(토스트/사운드 등)")]
    [SerializeField] private UnityEvent OnLockedClick;

    public event System.Action<int> Clicked; // (pageIndex)

    private bool _selected;

    private void Reset()
    {
        touchButton = GetComponentInChildren<TouchButton>(true);
    }

    private void Awake()
    {
        if (touchButton == null)
            touchButton = GetComponentInChildren<TouchButton>(true);

        if (touchButton == null)
        {
            Debug.LogError($"[BottomNavItem] TouchButton missing: {name}");
            enabled = false;
            return;
        }

        touchButton.ButtonTapped.AddListener(HandleTapped);
    }

    public void SetPageIndex(int index) => pageIndex = index;

    public void SetLocked(bool value)
    {
        locked = value;
        // 필요하면 여기서 잠금 비주얼 이벤트도 추가 가능(예: OnLockedStateChanged)
    }

    public bool IsLocked => locked;

    public void SetSelected(bool selected)
    {
        if (_selected == selected) return;
        _selected = selected;

        if (_selected) OnSelected?.Invoke();
        else OnDeselected?.Invoke();
    }


    private void HandleTapped()
    {
        if (locked)
        {
            OnLockedClick?.Invoke();
            return;
        }

        // 선택된 탭 재클릭 처리
        if (_selected)
        {
            if (allowRefreshOnReclick) OnReclickSelected?.Invoke();
            if (blockReclickWhenSelected && !allowRefreshOnReclick) return;
            if (blockReclickWhenSelected && allowRefreshOnReclick) return;
        }

        if (pageIndex < 0)
        {
            Debug.LogError($"[BottomNavItem] pageIndex not set: {name}");
            return;
        }

        OnClick?.Invoke();
        Clicked?.Invoke(pageIndex);
    }
}
