using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace AES.Tools
{
    [RequireComponent(typeof(Rect))]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("AES/Tools/Controls/Touch Button")]
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler, ISubmitHandler
    {
        [Header("Interaction")] 
        /// 이 버튼이 상호작용(클릭 등)이 가능한지 여부
        public bool interactable = true;
        
        public enum ButtonStates { Off, ButtonDown, ButtonPressed, ButtonUp, Disabled }

 		[Header("Binding")]
		/// 버튼이 처음 눌렸을 때 호출할 메서드(들)
		[Tooltip("버튼이 처음 눌렸을 때 호출할 메서드(들)")]
		public UnityEvent ButtonPressedFirstTime;
		/// 버튼에서 손을 뗐을 때 호출할 메서드(들)
		[Tooltip("버튼에서 손을 뗐을 때 호출할 메서드(들)")]
		public UnityEvent ButtonReleased;
		/// 버튼을 누르고 있는 동안 매 프레임 호출할 메서드(들)
		[Tooltip("버튼을 누르고 있는 동안 호출할 메서드(들)")]
		public UnityEvent ButtonPressed;
		
		/// 버튼을 '정상적으로 탭했을 때'(누르고 버튼 위에서 뗐을 때) 호출할 메서드(들)
		[Tooltip("버튼을 탭(Down+Up을 버튼 안에서 완료)했을 때 호출할 메서드(들)")]
		public UnityEvent ButtonTapped;

		[Header("Sprite Swap")]
		[AesInformation("Disabled / Pressed 상태에서 사용할 다른 스프라이트나 색을 정의할 수 있습니다.")]
		/// 버튼이 비활성화(Disabled) 상태일 때 사용할 스프라이트
		[Tooltip("버튼이 비활성화 상태일 때 사용할 스프라이트")]
		public Sprite DisabledSprite;
		/// 비활성화 상태일 때 색을 변경할지 여부
		[Tooltip("비활성화 상태일 때 버튼 색을 변경할지 여부")]
		public bool DisabledChangeColor = false;
		/// 비활성화 상태일 때 사용할 색
		[Tooltip("비활성화 상태일 때 사용할 색")]
		[ShowIf(nameof(DisabledChangeColor))]
		public Color DisabledColor = Color.gray5;
		/// 버튼이 눌린(Pressed) 상태일 때 사용할 스프라이트
		[Tooltip("버튼이 눌린 상태일 때 사용할 스프라이트")]
		public Sprite PressedSprite;
		/// 눌렸을 때 버튼 색을 변경할지 여부
		[Tooltip("눌렸을 때 버튼 색을 변경할지 여부")]
		public bool PressedChangeColor = false;
		/// 버튼이 눌린 상태일 때 사용할 색
		[Tooltip("버튼이 눌린 상태일 때 사용할 색")]
		[ShowIf(nameof(PressedChangeColor))]
		public Color PressedColor= Color.gray7;
		/// 버튼이 하이라이트(포커스) 상태일 때 사용할 스프라이트
		[Tooltip("버튼이 하이라이트 상태일 때 사용할 스프라이트")]
		public Sprite HighlightedSprite;
		/// 하이라이트 상태일 때 색을 변경할지 여부
		[Tooltip("하이라이트 상태일 때 버튼 색을 변경할지 여부")]
		public bool HighlightedChangeColor = false;
		/// 버튼이 하이라이트 상태일 때 사용할 색
		[Tooltip("버튼이 하이라이트 상태일 때 사용할 색")]
		[ShowIf(nameof(HighlightedChangeColor))]
		public Color HighlightedColor = Color.white;

		[Header("Opacity")]
		[AesInformation("버튼이 눌렸을 때 / 대기 중일 때 / 비활성화되었을 때 각각 다른 투명도를 설정할 수 있습니다. 시각적 피드백에 유용합니다.")]
		/// 버튼이 눌렸을 때 CanvasGroup에 적용할 투명도
		[Tooltip("버튼이 눌렸을 때 CanvasGroup에 적용할 투명도")]
		public float PressedOpacity = 1f;
		/// 버튼이 대기(Idle) 상태일 때 CanvasGroup에 적용할 투명도
		[Tooltip("버튼이 대기(Idle) 상태일 때 CanvasGroup에 적용할 투명도")]
		public float IdleOpacity = 1f;
		/// 버튼이 비활성화(Disabled) 상태일 때 CanvasGroup에 적용할 투명도
		[Tooltip("버튼이 비활성화 상태일 때 CanvasGroup에 적용할 투명도")]
		public float DisabledOpacity = 1f;

		[Header("Delays")]
		[AesInformation("버튼이 처음 눌렸을 때와 손을 뗐을 때 이벤트 호출에 지연 시간을 줄 수 있습니다. 보통은 0으로 둡니다.")]
		/// 버튼이 처음 눌렸을 때 이벤트 호출까지의 지연 시간
		[Tooltip("버튼이 처음 눌렸을 때 이벤트 호출까지의 지연 시간")]
		public float PressedFirstTimeDelay = 0f;
		/// 버튼에서 손을 뗐을 때 이벤트 호출까지의 지연 시간
		[Tooltip("버튼에서 손을 뗐을 때 이벤트 호출까지의 지연 시간")]
		public float ReleasedDelay = 0f;

		[Header("Buffer")]
		/// 버튼이 눌린 이후, 다시 누를 수 없게 막는 시간(초 단위, 입력 버퍼)
		[Tooltip("버튼이 눌린 이후, 다시 누를 수 없게 막는 시간(초 단위)")]
		public float BufferDuration = 0f;

		[Header("Animation")]
		[AesInformation("Animator를 연결해서 버튼 상태에 따라 애니메이션 파라미터를 업데이트할 수 있습니다.")]
		/// 버튼 상태를 반영하도록 바인딩할 Animator
		[Tooltip("버튼 상태를 반영하도록 바인딩할 Animator")]
		public Animator Animator;
		/// 버튼이 대기(Idle) 상태일 때 true로 설정할 애니메이션 파라미터 이름
		[Tooltip("버튼이 대기(Idle) 상태일 때 true로 설정할 애니메이션 파라미터 이름")]
		public string IdleAnimationParameterName = "Idle";
		/// 버튼이 비활성화(Disabled) 상태일 때 true로 설정할 애니메이션 파라미터 이름
		[Tooltip("버튼이 비활성화(Disabled) 상태일 때 true로 설정할 애니메이션 파라미터 이름")]
		public string DisabledAnimationParameterName = "Disabled";
		/// 버튼이 눌린(Pressed) 상태일 때 true로 설정할 애니메이션 파라미터 이름
		[Tooltip("버튼이 눌린(Pressed) 상태일 때 true로 설정할 애니메이션 파라미터 이름")]
		public string PressedAnimationParameterName = "Pressed";

		[Header("Mouse Mode")]
		[AesInformation("이 값을 true로 설정하면 실제로 클릭해야 버튼이 동작합니다. false라면 마우스를 올리는 것만으로도(hover) 트리거되므로, 터치 입력 위주의 UI라면 false가 더 잘 맞습니다.")]
		/// true면 실제 클릭해야 하고, false면 마우스 오버만으로도 트리거됩니다(터치 입력에 더 적합).
		[Tooltip("true면 실제 클릭해야 하고, false면 마우스 오버만으로도 트리거됩니다(터치 입력에 더 적합).")]
		public bool MouseMode = false;
		
		/// 왼쪽 클릭을 막을지 여부
		public bool PreventLeftClick = false;
		/// 가운데 클릭을 막을지 여부
		public bool PreventMiddleClick = true;
		/// 오른쪽 클릭을 막을지 여부
		public bool PreventRightClick = true;
		
		/// 버튼이 자동으로 원래 스프라이트로 돌아갈지 여부
		public virtual bool ReturnToInitialSpriteAutomatically { get; set; }

		/// 현재 버튼 상태 (Off, ButtonDown, ButtonPressed, ButtonUp, Disabled)
		public virtual ButtonStates CurrentState { get; protected set; }

		/// 버튼 상태 변경 시 알림을 보내는 이벤트 (PressState, PointerEventData)
		public event System.Action<PointerEventData.FramePressState, PointerEventData> ButtonStateChange;

		/// 터치/클릭 영역이 눌려있는지 여부
		protected bool _zonePressed = false;
		/// 버튼이 속한 CanvasGroup
		protected CanvasGroup _canvasGroup;
		/// 초기 투명도 값
		protected float _initialOpacity;
		/// 사용할 Animator 캐시
		protected Animator _animator;
		/// 버튼의 Image 컴포넌트
		protected Image _image;
		/// 버튼의 초기 스프라이트
		protected Sprite _initialSprite;
		/// 버튼의 초기 색
		protected Color _initialColor;
		/// 마지막 클릭 시각 (버퍼 체크용)
		protected float _lastClickTimestamp = 0f;
		/// 연결된 Selectable 컴포넌트 (있다면)
		protected Selectable _selectable;
		/// 포인터(손가락/마우스)가 현재 버튼 영역 안에 있는지 여부
		protected bool _pointerInside = false;
        
      /// <summary>
		/// Awake 시점에 CanvasGroup 등 필요한 컴포넌트를 가져와 초기 설정을 합니다.
		/// </summary>
		protected virtual void Awake()
		{
			Initialization ();
		}

		/// <summary>
		/// Image, Animator, CanvasGroup 등을 가져와 초기화합니다.
		/// </summary>
		protected virtual void Initialization()
		{
			ReturnToInitialSpriteAutomatically = true;

			_selectable = GetComponent<Selectable> ();

			_image = GetComponent<Image> ();
			if (_image != null)
			{
				_initialColor = _image.color;
				_initialSprite = _image.sprite;
			}

			_animator = GetComponent<Animator> ();
			if (Animator != null)
			{
				_animator = Animator;
			}

			_canvasGroup = GetComponent<CanvasGroup>();
			if (_canvasGroup!=null)
			{
				_initialOpacity = IdleOpacity;
				_canvasGroup.alpha = _initialOpacity;
				_initialOpacity = _canvasGroup.alpha;
			}
			ResetButton();
		}

		/// <summary>
		/// 매 프레임, 버튼 상태에 따라 처리합니다.
		/// Continuous press(누르고 있는 동안)를 위해 ButtonPressed 상태일 때 OnPointerPressed를 호출합니다.
		/// </summary>
		protected virtual void Update()
		{
			// interactable=false면 Disabled로 강제
			if (!interactable)
			{
				CurrentState = ButtonStates.Disabled;
			}
			// 다시 true가 되었고, 이전에 Disabled였으면 Off로 복귀
			else if (CurrentState == ButtonStates.Disabled)
			{
				CurrentState = ButtonStates.Off;
			}
				
			switch (CurrentState)
			{
				case ButtonStates.Off:
					SetOpacity (IdleOpacity);
					if ((_image != null) && (ReturnToInitialSpriteAutomatically))
					{
						_image.color = _initialColor;
						_image.sprite = _initialSprite;
					}
					if (_selectable != null)
					{
						_selectable.interactable = true;
						if (EventSystem.current.currentSelectedGameObject == gameObject)
						{
							if ((_image != null) && HighlightedChangeColor)
							{
								_image.color = HighlightedColor;
							}
							if (HighlightedSprite != null)
							{
								_image.sprite = HighlightedSprite;
							}
						}
					}
					break;

				case ButtonStates.Disabled:
					SetOpacity (DisabledOpacity);
					if (_image != null)
					{
						if (DisabledSprite != null)
						{
							_image.sprite = DisabledSprite;	
						}
						if (DisabledChangeColor)
						{
							_image.color = DisabledColor;	
						}
					}
					if (_selectable != null)
					{
						_selectable.interactable = false;
					}
					break;

				case ButtonStates.ButtonDown:
					// ButtonDown 상태는 1프레임 동안만 유지되고, LateUpdate에서 ButtonPressed로 전환됩니다.
					break;

				case ButtonStates.ButtonPressed:
					SetOpacity (PressedOpacity);
					OnPointerPressed();
					if (_image != null)
					{
						if (PressedSprite != null)
						{
							_image.sprite = PressedSprite;
						}
						if (PressedChangeColor)
						{
							_image.color = PressedColor;	
						}
					}
					break;

				case ButtonStates.ButtonUp:
					// ButtonUp 상태는 1프레임 동안만 유지되고, LateUpdate에서 Off로 전환됩니다.
					break;
			}
			UpdateAnimatorStates ();
		}

		/// <summary>
		/// 매 프레임 마지막(LateUpdate)에 ButtonDown → ButtonPressed, ButtonUp → Off 로 상태를 전환합니다.
		/// </summary>
		protected virtual void LateUpdate()
		{
			if (CurrentState == ButtonStates.ButtonUp)
			{
				CurrentState = ButtonStates.Off;
			}
			if (CurrentState == ButtonStates.ButtonDown)
			{
				CurrentState = ButtonStates.ButtonPressed;
			}
		}

		/// <summary>
		/// ButtonStateChange 이벤트를 지정된 상태로 호출합니다.
		/// </summary>
		public virtual void InvokeButtonStateChange(PointerEventData.FramePressState newState, PointerEventData data)
		{
			ButtonStateChange?.Invoke(newState, data);
		}

		/// <summary>
		/// MouseMode가 켜져 있을 때, 해당 클릭이 허용되는지 검사합니다.
		/// </summary>
		protected virtual bool AllowedClick(PointerEventData data)
		{
			if (!MouseMode)
			{
				return true;
			}
			if (PreventLeftClick && data.button == PointerEventData.InputButton.Left)
			{
				return false;
			}
			if (PreventMiddleClick && data.button == PointerEventData.InputButton.Middle)
			{
				return false;
			}
			if (PreventRightClick && data.button == PointerEventData.InputButton.Right)
			{
				return false;
			}
			return true;
		}
			
		/// <summary>
		/// 포인터가 눌렸을 때(마우스 다운 / 터치 시작) 호출됩니다.
		/// </summary>
		public virtual void OnPointerDown(PointerEventData data)
		{
			if (!interactable)
			{
				return;
			}

			if (!AllowedClick(data))
			{
				return;
			}
    
			// 포인터가 이 버튼을 실제로 누르기 시작했으니 안에 있다고 간주
			_pointerInside = true;
			
			// BufferDuration 안에 다시 눌리면 무시
			if (Time.unscaledTime - _lastClickTimestamp < BufferDuration)
			{
				return;
			}

			// 이미 다른 상태라면 무시
			if (CurrentState != ButtonStates.Off)
			{
				return;
			}
			CurrentState = ButtonStates.ButtonDown;
			_lastClickTimestamp = Time.unscaledTime;
			InvokeButtonStateChange(PointerEventData.FramePressState.Pressed, data);
			if ((Time.timeScale != 0) && (PressedFirstTimeDelay > 0))
			{
				Invoke ("InvokePressedFirstTime", PressedFirstTimeDelay);	
			}
			else
			{
				ButtonPressedFirstTime.Invoke();
			}
		}
		
		/// <summary>
		/// ButtonPressedFirstTime 이벤트를 호출합니다.
		/// </summary>
		protected virtual void InvokePressedFirstTime()
		{
			if (ButtonPressedFirstTime!=null)
			{
				ButtonPressedFirstTime.Invoke();
			}
		}

		/// <summary>
		/// 포인터에서 손을 뗐을 때(마우스 업 / 터치 종료) 호출됩니다.
		/// </summary>
		public virtual void OnPointerUp(PointerEventData data)
		{
			if (!interactable)
			{
				return;
			}
			if (!AllowedClick(data))
			{
				return;
			}
			// 눌려있던 상태가 아니라면 무시
			if (CurrentState != ButtonStates.ButtonPressed && CurrentState != ButtonStates.ButtonDown)
			{
				return;
			}

			CurrentState = ButtonStates.ButtonUp;
			InvokeButtonStateChange(PointerEventData.FramePressState.Released, data);

			// 1) 기존 Released 동작은 그대로 유지 (연출이 이걸 쓰고 있으니 손대지 않음)
			if ((Time.timeScale != 0) && (ReleasedDelay > 0))
			{
				Invoke(nameof(InvokeReleased), ReleasedDelay);
			}
			else
			{
				ButtonReleased?.Invoke();
			}

			// 2) 실제 "탭 입력"은 버튼 안에서 Up이 발생했을 때만 인정
			//    (밖으로 드래그해서 나간 경우: OnPointerExit에서 _pointerInside=false 후 Up 호출)
			if (_pointerInside && ButtonTapped != null)
			{
				ButtonTapped.Invoke();
			}
		}

		/// <summary>
		/// ButtonReleased 이벤트를 실제로 호출합니다.
		/// </summary>
		protected virtual void InvokeReleased()
		{
			if (ButtonReleased != null)
			{
				ButtonReleased.Invoke();
			}			
		}

		/// <summary>
		/// 버튼이 눌려 있는 동안 매 프레임 호출되는 처리입니다.
		/// </summary>
		public virtual void OnPointerPressed()
		{
			if (!interactable)
			{
				return;
			}
			CurrentState = ButtonStates.ButtonPressed;
			if (ButtonPressed != null)
			{
				ButtonPressed.Invoke();
			}
		}

		/// <summary>
		/// 버튼 상태와 투명도를 초기값으로 되돌립니다.
		/// </summary>
		protected virtual void ResetButton()
		{
			SetOpacity(_initialOpacity);
			CurrentState = ButtonStates.Off;
			_pointerInside = false;
		}

		/// <summary>
		/// 포인터가 버튼 영역에 들어왔을 때 호출됩니다.
		/// MouseMode가 false면 이 시점에 OnPointerDown을 자동으로 호출합니다.
		/// </summary>
		public virtual void OnPointerEnter(PointerEventData data)
		{
			if (!interactable)
			{
				return;
			}
			if (!AllowedClick(data))
			{
				return;
			}

			_pointerInside = true; // 버튼 안으로 들어옴

			if (!MouseMode)
			{
				OnPointerDown(data);
			}
		}


		/// <summary>
		/// 포인터가 버튼 영역을 벗어났을 때 호출됩니다.
		/// MouseMode가 false면 이 시점에 OnPointerUp을 자동으로 호출합니다.
		/// </summary>
		public virtual void OnPointerExit(PointerEventData data)
		{
			if (!interactable)
			{
				return;
			}
			if (!AllowedClick(data))
			{
				return;
			}

			_pointerInside = false; // 버튼 밖으로 나감

			if (!MouseMode)
			{
				// 기존 동작 유지: 나갈 때 자동으로 Up 처리 → ButtonReleased 그대로 동작
				OnPointerUp(data);
			}
		}

		/// <summary>
		/// 오브젝트가 활성화될 때 버튼 상태를 초기화합니다.
		/// </summary>
		protected virtual void OnEnable()
		{
			ResetButton();
		}

		/// <summary>
		/// 오브젝트가 비활성화될 때 버튼을 Disabled 처리하고, 눌려 있던 상태였다면 Released 이벤트를 보냅니다.
		/// </summary>
		private void OnDisable()
		{
			bool wasActive = CurrentState != ButtonStates.Off && CurrentState != ButtonStates.Disabled && CurrentState != ButtonStates.ButtonUp;
			DisableButton();
			CurrentState = ButtonStates.Off; 
			if (wasActive)
			{
				InvokeButtonStateChange(PointerEventData.FramePressState.Released, null);
				ButtonReleased?.Invoke();
			}
		}

		/// <summary>
		/// 버튼이 입력을 받지 못하도록 Disabled 상태로 만듭니다.
		/// </summary>
		public virtual void DisableButton()
		{
			CurrentState = ButtonStates.Disabled;
		}

		/// <summary>
		/// Disabled 상태의 버튼을 다시 활성화(입력 가능) 상태로 되돌립니다.
		/// </summary>
		public virtual void EnableButton()
		{
			if (CurrentState == ButtonStates.Disabled)
			{
				CurrentState = ButtonStates.Off;	
			}
		}

		/// <summary>
		/// CanvasGroup의 알파(투명도)를 지정한 값으로 설정합니다.
		/// </summary>
		protected virtual void SetOpacity(float newOpacity)
		{
			if (_canvasGroup!=null)
			{
				_canvasGroup.alpha = newOpacity;
			}
		}

		/// <summary>
		/// 현재 버튼 상태를 기준으로 Animator 파라미터 값을 갱신합니다.
		/// </summary>
		protected virtual void UpdateAnimatorStates ()
		{
			if (_animator == null)
			{
				return;
			}
			if (DisabledAnimationParameterName != null)
			{
				_animator.SetBool (DisabledAnimationParameterName, (CurrentState == ButtonStates.Disabled));
			}
			if (PressedAnimationParameterName != null)
			{
				_animator.SetBool (PressedAnimationParameterName, (CurrentState == ButtonStates.ButtonPressed));
			}
			if (IdleAnimationParameterName != null)
			{
				_animator.SetBool (IdleAnimationParameterName, (CurrentState == ButtonStates.Off));
			}
		}
		
		/// <summary>
		/// UI 시스템의 Submit 이벤트(예: 엔터 키, 패드 버튼 등)를 받았을 때 호출됩니다.
		/// </summary>
		public virtual void OnSubmit(BaseEventData eventData)
		{
			if (ButtonPressedFirstTime!=null)
			{
				ButtonPressedFirstTime.Invoke();
			}
			if (ButtonReleased!=null)
			{
				ButtonReleased.Invoke ();
			}
		}
    }
    
}


