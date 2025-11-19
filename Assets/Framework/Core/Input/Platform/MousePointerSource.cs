using UnityEngine.InputSystem;


namespace Core.Systems.Input.Platform
{
    public sealed class MousePointerSource : IPointerSource
    {
        public int TouchCount => Mouse.current != null && Mouse.current.leftButton.isPressed ? 1 : 0;

        public bool TryGetDown(out Pointer p)
        {
            p = default;
            var m = Mouse.current;
            if (m == null) return false;
            if (m.leftButton.wasPressedThisFrame) {
                p = new Pointer(-1, m.position.ReadValue());
                return true;
            }

            return false;
        }

        public bool TryGetHold(out Pointer p)
        {
            p = default;
            var m = Mouse.current;
            if (m == null) return false;
            if (m.leftButton.isPressed) {
                p = new Pointer(-1, m.position.ReadValue());
                return true;
            }

            return false;
        }

        public bool TryGetUp(out Pointer p)
        {
            p = default;
            var m = Mouse.current;
            if (m == null) return false;
            if (m.leftButton.wasReleasedThisFrame) {
                p = new Pointer(-1, m.position.ReadValue());
                return true;
            }

            return false;
        }

        public bool TryGetTwoTouches(out Pointer a, out Pointer b)
        {
            a = b = default;
            return false;
        } // 마우스는 미지원
    }
}