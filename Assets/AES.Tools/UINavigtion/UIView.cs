using UnityEngine;


public enum UIViewState
{
    Appearing,
    Appeared,
    Disappearing,
    Disappeared
}

public abstract class UIView : MonoBehaviour
{
    public UIViewState State { get; protected set; } = UIViewState.Disappeared;

    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private CanvasGroup canvasGroup;  // 없으면 Awake에서 GetComponent

    // 에디터에서 아무 데나 올려놓고, 런타임엔 특정 Root로 옮기고 싶다면:
    private Vector3 _initialLocalPos;
    private Transform _initialParent;

    protected virtual void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        _initialLocalPos = transform.localPosition;
        _initialParent = transform.parent;

        if (hideOnStart)
            InstantHide();
    }

    /// <summary>
    /// 에디터용 초기 위치로 리셋하고 싶을 때 호출 (필요 시)
    /// </summary>
    public virtual void ResetLayout()
    {
        transform.SetParent(_initialParent, false);
        transform.localPosition = _initialLocalPos;
    }

    public virtual void Show()
    {
        if (State == UIViewState.Appearing || State == UIViewState.Appeared)
            return;

        State = UIViewState.Appearing;
        gameObject.SetActive(true);

        OnShowStart();

        // 애니메이션 방식에 따라 변경 (코루틴, DoTween 등)
        // 여기선 기본 구현: 바로 나타남
        InstantShow();

        State = UIViewState.Appeared;
        OnShowComplete();
    }

    public virtual void Hide()
    {
        if (State == UIViewState.Disappearing || State == UIViewState.Disappeared)
            return;

        State = UIViewState.Disappearing;
        OnHideStart();

        // 기본 구현: 바로 숨김
        InstantHide();

        State = UIViewState.Disappeared;
        OnHideComplete();
        gameObject.SetActive(false);
    }

    protected virtual void InstantShow()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }

    protected virtual void InstantHide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

   
    protected virtual void OnShowStart() { }
    protected virtual void OnShowComplete() { }
    protected virtual void OnHideStart() { }
    protected virtual void OnHideComplete() { }
}
