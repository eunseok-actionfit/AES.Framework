using UnityEngine.InputSystem;


namespace AES.Tools.Platform
{
    public class TouchPointerSource : IPointerSource
    {
        public int TouchCount => Touchscreen.current?.touches.Count ?? 0;

        public bool TryGetDown(out Pointer p)
        {
            p = default;
            var ts = Touchscreen.current;
            if (ts == null) return false;
            var t = ts.primaryTouch;
            if (t.press.wasPressedThisFrame) {
                p = new Pointer(t.touchId.ReadValue(), t.position.ReadValue());
                return true;
            }

            return false;
        }

        public bool TryGetHold(out Pointer p)
        {
            p = default;
            var ts = Touchscreen.current;
            if (ts == null) return false;
            var t = ts.primaryTouch;
            if (t.press.isPressed) {
                p = new Pointer(t.touchId.ReadValue(), t.position.ReadValue());
                return true;
            }

            return false;
        }

        public bool TryGetUp(out Pointer p)
        {
            p = default;
            var ts = Touchscreen.current;
            if (ts == null) return false;
            var t = ts.primaryTouch;
            if (t.press.wasReleasedThisFrame) {
                p = new Pointer(t.touchId.ReadValue(), t.position.ReadValue());
                return true;
            }

            return false;
        }

        public bool TryGetTwoTouches(out Pointer a, out Pointer b)
        {
            a = b = default;
            var ts = Touchscreen.current;
            if (ts == null) return false;
            var touchs = ts.touches;
            if (touchs.Count < 2) return false;
            var t0 = touchs[0]; var t1 = touchs[1];
            if (!t0.press.isPressed || !t1.press.isPressed) return false;
            a = new Pointer(t0.touchId.ReadValue(), t0.position.ReadValue());
            b = new Pointer(t1.touchId.ReadValue(), t1.position.ReadValue());
            return false;
        }
    }
}