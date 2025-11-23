using UnityEngine;


namespace AES.Tools.Gesture
{
    public sealed class PinchGesture
    {
        private readonly float _minDelta;
        private readonly float _minStep;
        private float _lastDist;
        private bool _tracking;

        public PinchGesture(float minDistanceDelta, float minFactorStep)
        {
            _minDelta = minDistanceDelta;
            _minStep = minFactorStep;
        }

        public void Begin(Vector2 a, Vector2 b)
        {
            _tracking = true;
            _lastDist = Vector2.Distance(a, b);
        }

        public bool Update(Vector2 a, Vector2 b, out Vector2 center, out float factor)
        {
            center = (a + b) * 0.5f;
            factor = 1f;
            if (!_tracking) return false;
            var dist = Vector2.Distance(a, b);
            var delta = dist - _lastDist;
            if (Mathf.Abs(delta) >= _minDelta) {
                factor = 1f + Mathf.Sign(delta) * Mathf.Max(_minStep, Mathf.Abs(delta) / Mathf.Max(_lastDist, 1f));
                _lastDist = dist;
                return true;
            }

            return false;
        }

        public void End() { _tracking = false; }
        public void Cancel() { _tracking = false; }
    }
}