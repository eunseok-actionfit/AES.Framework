using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace AES.Tools.Controllers.Core
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("AES/Tools/Controls/Touch Button")]
    public class TouchButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler, ISubmitHandler,
        IPointerMoveHandler
    {
        [Header("Interaction")]
        public bool interactable = true;

        public enum ButtonStates { Off, ButtonDown, ButtonPressed, ButtonUp, Disabled }

        [Header("Binding")]
        public UnityEvent ButtonPressedFirstTime;
        public UnityEvent ButtonReleased;
        public UnityEvent ButtonPressed;
        public UnityEvent ButtonTapped;

        [Header("Sprite Swap")]
        public Sprite DisabledSprite;
        public bool DisabledChangeColor = false;
        [ShowIf(nameof(DisabledChangeColor))]
        public Color DisabledColor = Color.gray5;

        public Sprite PressedSprite;
        public bool PressedChangeColor = false;
        [ShowIf(nameof(PressedChangeColor))]
        public Color PressedColor = Color.gray7;

        public Sprite HighlightedSprite;
        public bool HighlightedChangeColor = false;
        [ShowIf(nameof(HighlightedChangeColor))]
        public Color HighlightedColor = Color.white;

        [Header("Opacity")]
        public float PressedOpacity = 1f;
        public float IdleOpacity = 1f;
        public float DisabledOpacity = 1f;

        [Header("Delays")]
        public float PressedFirstTimeDelay = 0f;
        public float ReleasedDelay = 0f;

        [Header("Buffer")]
        public float BufferDuration = 0f;

        [Header("Animation")]
        public Animator Animator;
        public string IdleAnimationParameterName = "Idle";
        public string DisabledAnimationParameterName = "Disabled";
        public string PressedAnimationParameterName = "Pressed";

        [Header("Mouse Mode")]
        public bool MouseMode = false;

        public bool PreventLeftClick = false;
        public bool PreventMiddleClick = true;
        public bool PreventRightClick = true;

        [Header("Tap Cancel (Drag Threshold)")]
        [Tooltip("드래그(스크롤)로 판단되면 Tap을 취소합니다.")]
        public bool CancelTapOnMove = false;

        [Tooltip("이 값이 0이면 EventSystem.pixelDragThreshold 사용")]
        public int MoveThresholdOverridePx = 0;

        [Tooltip("ScrollRect 안에 있을 때 MouseMode=false면 Enter에서 자동 Down을 막습니다.")]
        public bool DisableAutoDownOnEnterWhenInsideScrollRect = true;

        public virtual bool ReturnToInitialSpriteAutomatically { get; set; }
        public virtual ButtonStates CurrentState { get; protected set; }

        public event System.Action<PointerEventData.FramePressState, PointerEventData> ButtonStateChange;

        protected CanvasGroup _canvasGroup;
        protected float _initialOpacity;
        protected Animator _animator;
        protected Image _image;
        protected Sprite _initialSprite;
        protected Color _initialColor;
        protected float _lastClickTimestamp = 0f;
        protected Selectable _selectable;
        protected bool _pointerInside = false;

        // === Tap 취소용 (InputSystem-safe) ===
        private Vector2 _pressPosition;
        private bool _moved;
        private bool _pressed;
        private int _moveThresholdPx = 10;
        private ScrollRect _parentScrollRect;

        protected virtual void Awake()
        {
            Initialization();
        }

        protected virtual void Initialization()
        {
            ReturnToInitialSpriteAutomatically = true;

            _selectable = GetComponent<Selectable>();

            _image = GetComponent<Image>();
            if (_image != null)
            {
                _initialColor = _image.color;
                _initialSprite = _image.sprite;
            }

            _animator = GetComponent<Animator>();
            if (Animator != null) _animator = Animator;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null)
            {
                _initialOpacity = IdleOpacity;
                _canvasGroup.alpha = _initialOpacity;
                _initialOpacity = _canvasGroup.alpha;
            }

            _parentScrollRect = GetComponentInParent<ScrollRect>();

            if (MoveThresholdOverridePx > 0) _moveThresholdPx = MoveThresholdOverridePx;
            else if (EventSystem.current != null) _moveThresholdPx = EventSystem.current.pixelDragThreshold;

            ResetButton();
        }

        protected virtual void Update()
        {
            if (!interactable)
            {
                CurrentState = ButtonStates.Disabled;
            }
            else if (CurrentState == ButtonStates.Disabled)
            {
                CurrentState = ButtonStates.Off;
            }

            switch (CurrentState)
            {
                case ButtonStates.Off:
                    SetOpacity(IdleOpacity);
                    if ((_image != null) && ReturnToInitialSpriteAutomatically)
                    {
                        _image.color = _initialColor;
                        _image.sprite = _initialSprite;
                    }
                    if (_selectable != null)
                    {
                        _selectable.interactable = true;
                        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                        {
                            if ((_image != null) && HighlightedChangeColor) _image.color = HighlightedColor;
                            if (HighlightedSprite != null) _image.sprite = HighlightedSprite;
                        }
                    }
                    break;

                case ButtonStates.Disabled:
                    SetOpacity(DisabledOpacity);
                    if (_image != null)
                    {
                        if (DisabledSprite != null) _image.sprite = DisabledSprite;
                        if (DisabledChangeColor) _image.color = DisabledColor;
                    }
                    if (_selectable != null) _selectable.interactable = false;
                    break;

                case ButtonStates.ButtonDown:
                    break;

                case ButtonStates.ButtonPressed:
                    SetOpacity(PressedOpacity);
                    OnPointerPressed();
                    if (_image != null)
                    {
                        if (PressedSprite != null) _image.sprite = PressedSprite;
                        if (PressedChangeColor) _image.color = PressedColor;
                    }
                    break;

                case ButtonStates.ButtonUp:
                    break;
            }

            UpdateAnimatorStates();
        }

        protected virtual void LateUpdate()
        {
            if (CurrentState == ButtonStates.ButtonUp) CurrentState = ButtonStates.Off;
            if (CurrentState == ButtonStates.ButtonDown) CurrentState = ButtonStates.ButtonPressed;
        }

        public virtual void InvokeButtonStateChange(PointerEventData.FramePressState newState, PointerEventData data)
        {
            ButtonStateChange?.Invoke(newState, data);
        }

        protected virtual bool AllowedClick(PointerEventData data)
        {
            if (!MouseMode) return true;
            if (PreventLeftClick && data.button == PointerEventData.InputButton.Left) return false;
            if (PreventMiddleClick && data.button == PointerEventData.InputButton.Middle) return false;
            if (PreventRightClick && data.button == PointerEventData.InputButton.Right) return false;
            return true;
        }

        public virtual void OnPointerDown(PointerEventData data)
        {
            if (!interactable) return;
            if (!AllowedClick(data)) return;

            _pointerInside = true;

            // Tap 취소 판정 초기화
            _pressed = true;
            _pressPosition = data.position;
            _moved = false;

            if (Time.unscaledTime - _lastClickTimestamp < BufferDuration) return;
            if (CurrentState != ButtonStates.Off) return;

            CurrentState = ButtonStates.ButtonDown;
            _lastClickTimestamp = Time.unscaledTime;

            InvokeButtonStateChange(PointerEventData.FramePressState.Pressed, data);

            if ((Time.timeScale != 0) && (PressedFirstTimeDelay > 0))
                Invoke(nameof(InvokePressedFirstTime), PressedFirstTimeDelay);
            else
                ButtonPressedFirstTime?.Invoke();
        }

        // new Input System에서도 UI가 주는 position으로 이동 판정
        public void OnPointerMove(PointerEventData data)
        {
            if (!CancelTapOnMove) return;
            if (!_pressed) return;
            if (_moved) return;

            Vector2 delta = data.position - _pressPosition;
            if (delta.sqrMagnitude >= _moveThresholdPx * _moveThresholdPx)
            {
                _moved = true;
            }
        }

        protected virtual void InvokePressedFirstTime()
        {
            ButtonPressedFirstTime?.Invoke();
        }

        public virtual void OnPointerUp(PointerEventData data)
        {
            if (!interactable) return;
            if (!AllowedClick(data)) return;

            if (CurrentState != ButtonStates.ButtonPressed && CurrentState != ButtonStates.ButtonDown) return;

            _pressed = false;

            CurrentState = ButtonStates.ButtonUp;
            InvokeButtonStateChange(PointerEventData.FramePressState.Released, data);

            if ((Time.timeScale != 0) && (ReleasedDelay > 0))
                Invoke(nameof(InvokeReleased), ReleasedDelay);
            else
                ButtonReleased?.Invoke();

            // Tap: 버튼 안에서 Up + (이동 없을 때만)
            if (_pointerInside && ButtonTapped != null)
            {
                if (!(CancelTapOnMove && _moved))
                {
                    ButtonTapped.Invoke();
                }
            }
        }

        protected virtual void InvokeReleased()
        {
            ButtonReleased?.Invoke();
        }

        public virtual void OnPointerPressed()
        {
            if (!interactable) return;
            CurrentState = ButtonStates.ButtonPressed;
            ButtonPressed?.Invoke();
        }

        protected virtual void ResetButton()
        {
            SetOpacity(_initialOpacity);
            CurrentState = ButtonStates.Off;
            _pointerInside = false;
            _moved = false;
            _pressed = false;
        }

        public virtual void OnPointerEnter(PointerEventData data)
        {
            if (!interactable) return;
            if (!AllowedClick(data)) return;

            _pointerInside = true;

            if (!MouseMode)
            {
                if (DisableAutoDownOnEnterWhenInsideScrollRect && _parentScrollRect != null)
                    return;
                
                ExecuteEvents.Execute(gameObject, data, ExecuteEvents.pointerDownHandler);
            }
        }

        public virtual void OnPointerExit(PointerEventData data)
        {
            if (!interactable) return;
            if (!AllowedClick(data)) return;

            _pointerInside = false;

            if (!MouseMode)
            {
                ExecuteEvents.Execute(gameObject, data, ExecuteEvents.pointerUpHandler);
            }
        }


        protected virtual void OnEnable()
        {
            ResetButton();
        }

        private void OnDisable()
        {
            _pressed = false;

            bool wasActive = CurrentState != ButtonStates.Off
                             && CurrentState != ButtonStates.Disabled
                             && CurrentState != ButtonStates.ButtonUp;

            DisableButton();
            CurrentState = ButtonStates.Off;

            if (wasActive)
            {
                InvokeButtonStateChange(PointerEventData.FramePressState.Released, null);
                ButtonReleased?.Invoke();
            }
        }

        public virtual void DisableButton()
        {
            CurrentState = ButtonStates.Disabled;
        }

        public virtual void EnableButton()
        {
            if (CurrentState == ButtonStates.Disabled)
            {
                CurrentState = ButtonStates.Off;
            }
        }

        protected virtual void SetOpacity(float newOpacity)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = newOpacity;
            }
        }

        protected virtual void UpdateAnimatorStates()
        {
            if (_animator == null) return;

            if (!string.IsNullOrEmpty(DisabledAnimationParameterName))
                _animator.SetBool(DisabledAnimationParameterName, (CurrentState == ButtonStates.Disabled));

            if (!string.IsNullOrEmpty(PressedAnimationParameterName))
                _animator.SetBool(PressedAnimationParameterName, (CurrentState == ButtonStates.ButtonPressed));

            if (!string.IsNullOrEmpty(IdleAnimationParameterName))
                _animator.SetBool(IdleAnimationParameterName, (CurrentState == ButtonStates.Off));
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            ButtonPressedFirstTime?.Invoke();
            ButtonReleased?.Invoke();
        }
    }
}
