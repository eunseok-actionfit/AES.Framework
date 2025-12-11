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
    public int     fontSize       = 14;

    [Header("Click Picking")]
    [Tooltip("클릭으로 target 자동 선택")]
    public bool pickOnClick = true;

    [Tooltip("true면 Physics2D 사용, false면 3D Physics 사용")]
    public bool use2DPhysics = false;

    [Tooltip("Raycast에 사용할 카메라 (비워두면 Camera.main)")]
    public Camera raycastCamera;

    [Tooltip("클릭 가능한 레이어 마스크")]
    public LayerMask pickLayerMask = ~0;

    StateMachine _machine;

    string _current   = "";
    string _previous  = "";
    string _transition = "";

    void OnEnable()
    {
        AttachTarget(target);
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            AttachTarget(target);
    }

    void OnDestroy()
    {
        Detach();
    }

    //========================================================
    // 런타임 클릭으로 target 자동 선택
    //========================================================

    void Update()
    {
        if (!Application.isPlaying)
            return;

        if (!pickOnClick)
            return;

        if (WasPointerDownThisFrame())
            TryPickTargetFromClick();
    }


    bool WasPointerDownThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        // 1) New Input System - Touch
        if (Touchscreen.current != null)
        {
            var ts = Touchscreen.current;
            if (ts.primaryTouch.press.wasPressedThisFrame)
                return true;
        }

        // 2) New Input System - Mouse
        if (Mouse.current != null)
        {
            var m = Mouse.current;
            if (m.leftButton.wasPressedThisFrame)
                return true;
        }

        // New Input System 전용 모드에서는 여기서 종료
        return false;

#elif ENABLE_LEGACY_INPUT_MANAGER
    // 3) Legacy Input System - Touch
    if (UnityEngine.Input.touchCount > 0)
    {
        var t = UnityEngine.Input.GetTouch(0);
        if (t.phase == TouchPhase.Began)
            return true;
    }

    // 4) Legacy Input System - Mouse
    return UnityEngine.Input.GetMouseButtonDown(0);
#else
    // 둘 다 아닐 경우(이론상)
    return false;
#endif
    }

    
    void TryPickTargetFromClick()
    {
        var cam = raycastCamera != null ? raycastCamera : Camera.main;
        if (cam == null)
            return;

        Vector3 pointerPos;

// New Input System
#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            pointerPos = Mouse.current.position.ReadValue();
        }
        else
        {
            // fallback to legacy
            if (Input.touchCount > 0)
                pointerPos = Input.GetTouch(0).position;
            else
                pointerPos = Input.mousePosition;
        }
#else
// Legacy만 쓸 때
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

    void AttachTarget(MonoBehaviour newTarget)
    {
        Detach();

        target = newTarget;

        if (newTarget is not IStateMachineOwner owner)
        {
            _machine = null;
            return;
        }

        _machine = owner.Machine;

        if (_machine != null)
            _machine.OnStateChanged += OnStateChanged;

#if UNITY_EDITOR
        // 에디터에서 Hierarchy/Scene Selection도 같이 맞춰준다.
        if (newTarget != null)
            Selection.activeGameObject = newTarget.gameObject;
#endif
    }


    void Detach()
    {
        if (_machine != null)
            _machine.OnStateChanged -= OnStateChanged;

        _machine = null;
    }

    //========================================================
    // 상태 변경 이벤트 처리
    //========================================================

    void OnStateChanged(IState from, IState to, Transition t)
    {
        _previous   = from?.GetType().Name ?? "null";
        _current    = to?.GetType().Name   ?? "null";
        _transition = t?.Name ?? t?.GetType().Name ?? "SetState";
    }

    //========================================================
    // HUD 표시
    //========================================================

    void OnGUI()
    {
        if (_machine == null)
            return;

        // 박스 스타일
        var boxStyle = new GUIStyle(GUI.skin.box);
    
        // 텍스트 스타일
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.UpperLeft
        };
        labelStyle.normal.textColor = Color.white;

        // Unity IMGUI의 실제 줄 높이 (fontSize + 내부기반 lineSpacing)
        float lineHeight = labelStyle.lineHeight;

        // 출력할 텍스트 목록
        string[] lines =
        {
            $"Target    : {target?.name}",
            $"Current   : {_current}",
            $"Previous  : {_previous}",
            $"Transition: {_transition}"
        };

        // 가장 긴 텍스트 width 계산 (fontSize 포함)
        float maxTextWidth = 0f;
        foreach (var line in lines)
        {
            float w = labelStyle.CalcSize(new GUIContent(line)).x;
            if (w > maxTextWidth) maxTextWidth = w;
        }

        // 텍스트 부분 실제 높이 (줄 수 × 줄 높이)
        float totalTextHeight = lines.Length * lineHeight;

        // labelStyle.padding과 boxStyle.padding 자동 반영
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
