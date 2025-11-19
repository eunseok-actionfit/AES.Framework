using UnityEngine;


namespace Core.Systems.Input.Platform
{
    public interface IPointerSource
    {
        bool TryGetDown(out Pointer p);
        bool TryGetHold(out Pointer p);
        bool TryGetUp(out Pointer p);
        // 멀티터치 지원용(핀치)
        int TouchCount { get; }
        bool TryGetTwoTouches(out Pointer a, out Pointer b);
    }

    public readonly struct Pointer
    {
        public readonly int id;           // mouse:-1, touch:fingerId
        public readonly Vector2 position; // screen
        public Pointer(int id, Vector2 pos) { this.id = id; this.position = pos; }
    }
}

