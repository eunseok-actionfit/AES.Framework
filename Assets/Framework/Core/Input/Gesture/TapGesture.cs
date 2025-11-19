using UnityEngine;


namespace Core.Systems.Input.Gesture
{
    public sealed class TapGesture
    {
        private readonly float _maxMoveSqr;
        private readonly float _maxTime;
        private Vector2 _start;
        private float _t;
        private bool _track;

        public TapGesture(float maxMove, float maxTime)
        {
            _maxMoveSqr = maxMove * maxMove;
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

        public bool TryEnd(Vector2 p)
        {
            var ok = _track && _t <= _maxTime && (p - _start).sqrMagnitude <= _maxMoveSqr;
            _track = false;
            return ok;
        }

        public void Cancel() { _track = false; }
    }
}