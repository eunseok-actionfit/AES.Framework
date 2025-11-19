using System;
using UnityEngine;


namespace Core.Systems.Input
{
    public interface IInputService
    {
        bool Enabled { get; set; }

        // 상태
        bool IsPressed { get; }
        Vector2 Position { get; }
        Vector2 Delta { get; }

        // 제스처 이벤트
        event Action<Vector2> OnTap;
        event Action<Vector2> OnDoubleTap;
        event Action<Vector2, float> OnLongPress;
        event Action<Vector2, Vector2> OnSwipe;

         /// <summary>
        /// 두 터치 포인트가 더 가까이 또는 더 멀리 이동하면 핀치 제스처 트리거
        /// </summary>
        /// <remarks>
        /// 이 이벤트는 핀치 제스처의 중심 위치와 스케일링 계수를 제공합니다.
        /// 두 터치 포인트 사이의 거리의 상대적 변화를 나타냅니다.
        /// 중앙 위치를 사용하여 핀치가 발생하는 위치를 결정하고 요인
        /// 스케일 변경을 측정합니다 (예 : 확대 또는 축소의 경우).
        /// </remarks>
        /// <param name = "vector2"> 2D 공간에서 핀치의 중심(center) 위치. </param>
        /// <param name = "float"> 스케일링 계수(factor), 여기서 1보다 큰 값은 확대/축소를 나타내고 1 미만의 축소가 나타납니다. </param>
        event Action<Vector2, float> OnPinch; // center, factor
        event Action<Vector2> OnPointerDown;

        /// <summary>
        /// 포인터 드래그 이벤트 중에, 포인터가 고정되는 동안 움직일 때 발생합니다.
        /// </summary>
        /// <remarks>
        /// 이벤트는 현재 포인터 위치와 위치 변경 (델타)을 제공합니다.
        /// 마지막 프레임 또는 업데이트 이후.
        /// </remarks>
        /// <param name = "vector2"> 2D 공간에서 포인터의 현재 위치 (ScreenPos). </param>
        /// <param name = "vector2"> 포인터의 위치 델타 (위치 변경). </param>
        event Action<Vector2, Vector2> OnPointerDrag; // pos, delta
        event Action<Vector2> OnPointerUp;
        

        void Tick(float dt);
    }
}