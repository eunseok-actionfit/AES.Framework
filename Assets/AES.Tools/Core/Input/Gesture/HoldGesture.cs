using UnityEngine;


namespace AES.Tools.Gesture
{
    public sealed class HoldGesture
    {
        private readonly float _time;
        private readonly float _tolSqr;
        private Vector2 _start;
        private float _t;
        private bool _track;
        private bool _fired;

        public HoldGesture(float time, float tol)
        {
            _time = time;
            _tolSqr = tol * tol;
        }

        public void Begin(Vector2 p)
        {
            _track = true;
            _fired = false;
            _t = 0;
            _start = p;
        }

        public bool Update(Vector2 pos, float dt, out Vector2 holdPos, out float dur)
        {
            holdPos = pos;
            dur = 0f;
            if (!_track) return false;
            _t += dt;
            if (!_fired && _t >= _time && (pos - _start).sqrMagnitude <= _tolSqr) {
                _fired = true;
                dur = _t;
                holdPos = pos;
                return true;
            }

            return false;
        }

        public void End() { _track = false; }
        public void Cancel() { _track = false; }
    }
}