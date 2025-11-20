using UnityEngine;


namespace AES.Tools.Gesture
{
    public sealed class SwipeGesture
    {
        private readonly float _minDistSqr;
        private readonly float _maxTime;
        private Vector2 _start;
        private float _t;
        private bool _track;

        public SwipeGesture(float minDist, float maxTime)
        {
            _minDistSqr = minDist * minDist;
            _maxTime = maxTime;
        }

        public void Begin(Vector2 p)
        {
            _track = true;
            _start = p;
            _t = 0;
        }

        public void Update(float dt)
        {
            if (_track) _t += dt;
        }

        public bool TryEnd(Vector2 p, out Vector2 from, out Vector2 to)
        {
            from = _start;
            to = p;
            var ok = _track && _t <= _maxTime && (p - _start).sqrMagnitude >= _minDistSqr;
            _track = false;
            return ok;
        }

        public void Cancel() { _track = false; }
    }
}