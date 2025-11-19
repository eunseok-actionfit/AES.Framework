using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Systems.UI.Guards
{
    public sealed class InputGuardService : IInputGuard
    {
        private readonly Dictionary<string, float> _last = new();
        private readonly HashSet<string> _active = new();
        private readonly Func<float> _time;

        public InputGuardService(Func<float> timeProvider = null)
        {
            _time = timeProvider ?? (() => Time.unscaledTime);
        }

        public bool Throttle(string id, float seconds = 0.3f)
        {
            var now = _time();
            if (_last.TryGetValue(id, out var last) && (now - last) < seconds)
                return false;

            _last[id] = now;
            return true;
        }

        public (bool, Action) Debounce(string id)
        {
            if (!_active.Add(id)) return (false, null);
            return (true, () => _active.Remove(id));
        }

        public void Reset()
        {
            _last.Clear();
            _active.Clear();
        }
    }
}