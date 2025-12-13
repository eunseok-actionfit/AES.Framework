// using System;
// using AES.Tools.Core;
// using UnityEngine;
//
//
// namespace AES.Tools
// {
//     /// <summary>
//     /// 전역 입력 파사드.
//     /// 내부적으로 IInputService 를 사용하며, DI/Non-DI 환경 모두에서 동일한 API를 제공한다.
//     /// </summary>
//     public static class Input
//     {
//         // 실제 구현을 들고 있는 서비스(Locator/Holder에서 가져옴)
//         private static IInputService S => InputServiceLocator.Service;
//
//         /// <summary>입력 처리 On/Off</summary>
//         public static bool Enabled
//         {
//             get => S.Enabled;
//             set => S.Enabled = value;
//         }
//
//         /// <summary>현재 어떤 포인터가 눌려 있는지 여부</summary>
//         public static bool IsPressed => S.IsPressed;
//
//         /// <summary>현재 포인터 위치(Screen 좌표)</summary>
//         public static Vector2 Position => S.Position;
//
//         /// <summary>마지막 프레임 대비 포인터 이동량</summary>
//         public static Vector2 Delta => S.Delta;
//
//         // ───────────────────────────────── 제스처 이벤트 ─────────────────────────────────
//
//         public static event Action<Vector2> OnTap
//         {
//             add    => S.OnTap += value;
//             remove => S.OnTap -= value;
//         }
//
//         public static event Action<Vector2> OnDoubleTap
//         {
//             add    => S.OnDoubleTap += value;
//             remove => S.OnDoubleTap -= value;
//         }
//
//         public static event Action<Vector2, float> OnLongPress
//         {
//             add    => S.OnLongPress += value;
//             remove => S.OnLongPress -= value;
//         }
//
//         public static event Action<Vector2, Vector2> OnSwipe
//         {
//             add    => S.OnSwipe += value;
//             remove => S.OnSwipe -= value;
//         }
//
//         /// <summary>
//         /// 두 터치 포인트가 더 가까이 또는 더 멀리 이동하면 핀치 제스처 트리거.
//         /// center: 핀치 중심 위치, factor: 1보다 크면 확대, 작으면 축소.
//         /// </summary>
//         public static event Action<Vector2, float> OnPinch
//         {
//             add    => S.OnPinch += value;
//             remove => S.OnPinch -= value;
//         }
//
//         // ───────────────────────────────── 포인터 이벤트 ─────────────────────────────────
//
//         public static event Action<Vector2> OnPointerDown
//         {
//             add    => S.OnPointerDown += value;
//             remove => S.OnPointerDown -= value;
//         }
//
//         /// <summary>
//         /// 포인터가 눌린 상태에서 움직일 때 발생.
//         /// pos: 현재 위치, delta: 마지막 프레임 대비 이동량.
//         /// </summary>
//         public static event Action<Vector2, Vector2> OnPointerDrag
//         {
//             add    => S.OnPointerDrag += value;
//             remove => S.OnPointerDrag -= value;
//         }
//
//         public static event Action<Vector2> OnPointerUp
//         {
//             add    => S.OnPointerUp += value;
//             remove => S.OnPointerUp -= value;
//         }
//     }
// }
