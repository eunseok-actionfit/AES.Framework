using AES.Tools;
using UnityEngine;
using UnityEngine.InputSystem;
using Input = UnityEngine.Input;

#if UNITY_EDITOR
using UnityEditor;
#endif


public sealed class StateMachineDebugHud : MonoBehaviour
{
    [Tooltip("IStateMachineOwner를 구현한 컴포넌트 (직접 지정 or 클릭 자동 선택)")]
    public MonoBehaviour target;

    [Header("HUD")]
    public Vector2 screenPosition = new(10, 10);
    public int fontSize = 14;

    [Header("Click Picking")]
    [Tooltip("클릭으로 target 자동 선택")]
    public bool pickOnClick = true;

    [Tooltip("true면 Physics2D 사용, false면 3D Physics 사용")]
    public bool use2DPhysics = false;

    [Tooltip("Raycast에 사용할 카메라 (비워두면 Camera.main)")]
    public Camera raycastCamera;

    [Tooltip("클릭 가능한 레이어 마스크")]
    public LayerMask pickLayerMask = ~0;

    private StateMachine _machine;

    private string _current = "";
    private string _previous = "";
    private string _transition = "";

    private void OnEnable()
    {
        AttachTarget(target);
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            AttachTarget(target);
    }

    private void OnDestroy()
    {
        Detach();
    }

    //========================================================
    // 런타임 클릭으로 target 자동 선택
    //========================================================

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!pickOnClick)
            return;

        if (WasPointerDownThisFrame())
            TryPickTargetFromClick();
    }

    private bool WasPointerDownThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            var ts = Touchscreen.current;
            if (ts.primaryTouch.press.wasPressedThisFrame)
                return true;
        }

        if (Mouse.current != null)
        {
            var m = Mouse.current;
            if (m.leftButton.wasPressedThisFrame)
                return true;
        }

        return false;

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (UnityEngine.Input.touchCount > 0)
        {
            var t = UnityEngine.Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
                return true;
        }

        return UnityEngine.Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private void TryPickTargetFromClick()
    {
        var cam = raycastCamera != null ? raycastCamera : Camera.main;
        if (cam == null)
            return;

        Vector3 pointerPos;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
            pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
        else if (Mouse.current != null)
            pointerPos = Mouse.current.position.ReadValue();
        else
        {
            if (Input.touchCount > 0) pointerPos = Input.GetTouch(0).position;
            else pointerPos = Input.mousePosition;
        }
#else
        if (UnityEngine.Input.touchCount > 0)
            pointerPos = UnityEngine.Input.GetTouch(0).position;
        else
            pointerPos = UnityEngine.Input.mousePosition;
#endif

        if (use2DPhysics)
        {
            var worldPos = cam.ScreenToWorldPoint(pointerPos);
            var hit = Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, pickLayerMask);
            if (hit.collider == null)
                return;

            var owner = hit.collider.GetComponentInParent<IStateMachineOwner>();
            if (owner is MonoBehaviour mb)
                AttachTarget(mb);
        }
        else
        {
            var ray = cam.ScreenPointToRay(pointerPos);

            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, pickLayerMask))
            {
                var owner = hit.collider.GetComponentInParent<IStateMachineOwner>();
                if (owner is MonoBehaviour mb)
                    AttachTarget(mb);
            }
        }
    }

    //========================================================
    // Target 연결 / 해제
    //========================================================

    private void AttachTarget(MonoBehaviour newTarget)
    {
        Detach();

        target = newTarget;

        if (newTarget is not IStateMachineOwner owner)
        {
            _machine = null;
            return;
        }

        _machine = owner.Machine;
        if (_machine == null)
            return;

        // (prev, cur) 이벤트로 구독
        _machine.CurrentState.OnValueChangedWithPrev += OnStateChangedWithPrev;

        // transition은 StateMachine에서 따로 프로퍼티로 노출된 걸로 구독
        _machine.LastTransitionName.OnValueChanged += OnTransitionChanged;

        // 초기 HUD 값 세팅
        _current = _machine.CurrentState.Value?.GetType().Name ?? "null";
        _previous = "null";
        _transition = _machine.LastTransitionName.Value ?? "SetState";

#if UNITY_EDITOR
        if (newTarget != null)
            Selection.activeGameObject = newTarget.gameObject;
#endif
    }

    private void Detach()
    {
        if (_machine != null)
        {
            _machine.CurrentState.OnValueChangedWithPrev -= OnStateChangedWithPrev;
            _machine.LastTransitionName.OnValueChanged -= OnTransitionChanged;
        }

        _machine = null;
    }

    //========================================================
    // 상태 변경 이벤트 처리
    //========================================================

    private void OnStateChangedWithPrev(IState from, IState to)
    {
        _previous = from?.GetType().Name ?? "null";
        _current = to?.GetType().Name ?? "null";
    }

    private void OnTransitionChanged(string name)
    {
        _transition = string.IsNullOrEmpty(name) ? "SetState" : name;
    }

    //========================================================
    // HUD 표시
    //========================================================

    private void OnGUI()
    {
        if (_machine == null)
            return;

        var boxStyle = new GUIStyle(GUI.skin.box);

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.UpperLeft
        };

        labelStyle.normal.textColor = Color.white;

        float lineHeight = labelStyle.lineHeight;

        string[] lines =
        {
            $"Target    : {target?.name}",
            $"Current   : {_current}",
            $"Previous  : {_previous}",
            $"Transition: {_transition}"
        };

        float maxTextWidth = 0f;

        foreach (var line in lines)
        {
            float w = labelStyle.CalcSize(new GUIContent(line)).x;
            if (w > maxTextWidth) maxTextWidth = w;
        }

        float totalTextHeight = lines.Length * lineHeight;

        float width =
            maxTextWidth +
            boxStyle.padding.left +
            boxStyle.padding.right +
            labelStyle.padding.left +
            labelStyle.padding.right +
            10;

        float height =
            totalTextHeight +
            boxStyle.padding.top +
            boxStyle.padding.bottom +
            labelStyle.padding.top +
            labelStyle.padding.bottom +
            fontSize;

        var rect = new Rect(screenPosition, new Vector2(width, height));

        GUILayout.BeginArea(rect, boxStyle);
        foreach (var line in lines)
            GUILayout.Label(line, labelStyle);

        GUILayout.EndArea();
    }
}