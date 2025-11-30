// using System;
// using System.Collections.Generic;
// using AES.Tools;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.EventSystems;
// using UnityEngine.InputSystem;
// using static PlayerInputActions;
//
// public interface IInputReader
// {
//     void EnablePlayerActions();
// }
//
// [CreateAssetMenu(fileName = "InputReader", menuName = "Input/Input Reader")]
// [Serializable]
// public class InputReader : ScriptableObject, IPlayerActions, IInputReader
// {
//     [Header("UI 차단")]
//     [SerializeField] bool blockWhenPointerOverUI = true;
//
//     [Header("탭 / 더블탭 / 스와이프 파라미터")]
//     [Tooltip("탭으로 인정할 최대 누르고 있는 시간(초)")]
//     [SerializeField] float maxTapDuration = 0.2f;
//
//     [Tooltip("탭으로 인정할 수 있는 최대 이동 거리(월드 단위)")]
//     [SerializeField] float maxTapMoveDistance = 0.2f;
//
//     [Tooltip("더블탭으로 인정할 탭 간 최대 시간(초)")]
//     [SerializeField] float doubleTapMaxDelay = 0.25f;
//
//     [Tooltip("스와이프로 인정할 최소 이동 거리(월드 단위)")]
//     [SerializeField] float swipeMinDistance = 0.5f;
//
//     // 기본 포인터 이벤트(Interactor가 구독)
//     public event UnityAction<Vector2> OnPointerDown = delegate { };
//     public event UnityAction<Vector2> OnPointerMove = delegate { };
//     public event UnityAction<Vector2> OnPointerUp   = delegate { };
//
//     public event UnityAction OnInputCanceled = delegate { };
//
//     // 제스처 이벤트
//     public event UnityAction<Vector2> OnTap            = delegate { };
//     public event UnityAction<Vector2> OnDoubleTap      = delegate { };
//     public event UnityAction<Vector2, Vector2> OnSwipe = delegate { };
//
//     PlayerInputActions _actions;
//
//     // 내부 상태
//     bool _isPressing;
//     Vector2 _pressStartWorld;
//     Vector2 _pressStartScreen;
//     float _pressStartTime;
//
//     bool _movedBeyondTapThreshold;
//
//     Vector2 _lastTapWorld;
//     float _lastTapTime = -999f;
//
//     // ===== IInputReader =====
//     public void EnablePlayerActions()
//     {
//         if (_actions == null)
//         {
//             _actions = new PlayerInputActions();
//             _actions.Player.SetCallbacks(this);
//         }
//
//         _actions.Enable();
//     }
//
//     public void DisablePlayerActions() => _actions?.Disable();
//
//     // ===== Input System 콜백 (IPlayerActions) =====
//     public void OnPoint(InputAction.CallbackContext ctx)
//     {
//         var screen = ctx.ReadValue<Vector2>();
//         var world = ScreenToWorld(screen);
//
//         if (_isPressing)
//         {
//             var moveDist = Vector2.Distance(world, _pressStartWorld);
//             if (moveDist > maxTapMoveDistance)
//                 _movedBeyondTapThreshold = true;
//         }
//
//         OnPointerMove(world);
//     }
//
//     public void OnPress(InputAction.CallbackContext ctx)
//     {
//         // UI 위면 입력 취소
//         if (IsOverUI(CurrentScreen()))
//             return;
//
//         if (ctx.started)
//         {
//             _isPressing = true;
//             _pressStartTime = Time.time;
//             _pressStartScreen = CurrentScreen();
//             _pressStartWorld = CurrentWorld();
//             _movedBeyondTapThreshold = false;
//
//             OnPointerDown(_pressStartWorld);
//         }
//         else if (ctx.canceled)
//         {
//             var releaseWorld = CurrentWorld();
//             OnPointerUp(releaseWorld);
//
//             if (_isPressing)
//             {
//                 HandleGesture(_pressStartWorld, releaseWorld, Time.time - _pressStartTime);
//             }
//
//             _isPressing = false;
//         }
//     }
//
//     // ===== 제스처 판정 =====
//     void HandleGesture(Vector2 startWorld, Vector2 endWorld, float duration)
//     {
//         var moveDistance = Vector2.Distance(startWorld, endWorld);
//
//         bool isTapCandidate =
//             !_movedBeyondTapThreshold &&
//             duration <= maxTapDuration &&
//             moveDistance <= maxTapMoveDistance;
//
//         if (isTapCandidate)
//         {
//             // 탭
//             OnTap(endWorld);
//
//             // 더블탭
//             if (Time.time - _lastTapTime <= doubleTapMaxDelay &&
//                 Vector2.Distance(_lastTapWorld, endWorld) <= maxTapMoveDistance)
//             {
//                 OnDoubleTap(endWorld);
//             }
//
//             _lastTapTime = Time.time;
//             _lastTapWorld = endWorld;
//         }
//         else
//         {
//             // 스와이프
//             if (moveDistance >= swipeMinDistance)
//             {
//                 OnSwipe(startWorld, endWorld);
//             }
//         }
//     }
//
//     // ===== 헬퍼 =====
//     Vector2 CurrentScreen() => _actions.Player.Point.ReadValue<Vector2>();
//
//     Vector2 CurrentWorld() => ScreenToWorld(CurrentScreen());
//
//     Vector2 ScreenToWorld(Vector3 screen)
//     {
//         var cam = Camera.main;
//         if (!cam) return Vector2.zero;
//
//         var w = cam.ScreenToWorldPoint(screen.With(z:0f));
//         return new Vector2(w.x, w.y);
//     }
//
//     bool IsOverUI(Vector2 screen)
//     {
//         if (!blockWhenPointerOverUI) return false;
//         if (EventSystem.current == null) return false;
//
//         var data = new PointerEventData(EventSystem.current) { position = screen };
//         var results = new List<RaycastResult>();
//
//         EventSystem.current.RaycastAll(data, results);
//         if (results.Count == 0) return false;
//
//         OnInputCanceled();
//         return true;
//     }
// }
