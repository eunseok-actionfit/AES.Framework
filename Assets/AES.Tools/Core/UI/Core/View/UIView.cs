using UnityEngine;
using UnityEngine.Events;

namespace AES.Tools.View
{
    public enum UIViewState
    {
        Appearing,
        Appeared,
        Disappearing,
        Disappeared
    }

    [RequireComponent(typeof(CanvasGroup))]
    public class UIView : MonoBehaviour
    {
        public UIViewState State { get; protected set; } = UIViewState.Disappeared;
        
        [SerializeField] CanvasGroup canvasGroup;

        [Header("Events")]
        public UnityEvent OnShowStarted;
        public UnityEvent OnShowCompleted;
        public UnityEvent OnHideStarted;
        public UnityEvent OnHideCompleted;

        protected virtual void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        // ======================================================
        // Show
        // ======================================================
        public virtual void Show()
        {
            // if (State == UIViewState.Appearing || State == UIViewState.Appeared)
            //     return;

            State = UIViewState.Appearing;
            gameObject.SetActive(true);

            // UnityEvent 호출
            OnShowStarted?.Invoke();

            InstantShow();

            State = UIViewState.Appeared;

            // UnityEvent 호출
            OnShowCompleted?.Invoke();
        }

        // ======================================================
        // Hide
        // ======================================================
        public virtual void Hide()
        {
            // if (State == UIViewState.Disappearing || State == UIViewState.Disappeared)
            //     return;

            State = UIViewState.Disappearing;

            // UnityEvent 호출
            OnHideStarted?.Invoke();

            InstantHide();

            State = UIViewState.Disappeared;
            gameObject.SetActive(false);

            // UnityEvent 호출
            OnHideCompleted?.Invoke();
        }

        // ======================================================
        // Instant Toggles
        // ======================================================

        protected virtual void InstantShow()
        {
            if (!canvasGroup) return;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        protected virtual void InstantHide()
        {
            if (!canvasGroup) return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
