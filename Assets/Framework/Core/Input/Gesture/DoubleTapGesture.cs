using UnityEngine;


namespace Core.Systems.Input.Gesture
{
    public sealed class DoubleTapGesture
    {
        private readonly float _gap;
        private readonly float _maxMoveSqr;
        private float _t;
        private Vector2 _lastPos;
        private bool _armed;

        public DoubleTapGesture(float gap, float maxMove)
        {
            _gap = gap;
            _maxMoveSqr = maxMove * maxMove;
        }

        public void OnTap(Vector2 p)
        {
            if (_armed && _t <= _gap && (p - _lastPos).sqrMagnitude <= _maxMoveSqr) {
                _armed = false;
                _t = 0;
                OnDoubleTap?.Invoke(p);
            }
            else {
                _armed = true;
                _t = 0;
                _lastPos = p;
            }
        }

        public void Update(float dt)
        {
            if (_armed) _t += dt;
            if (_t > _gap) _armed = false;
        }

        public event System.Action<Vector2> OnDoubleTap;
    }
}