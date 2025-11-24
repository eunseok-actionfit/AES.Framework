using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AES.Tools
{
    /// <summary>
    /// INotifyPropertyChanged 기반 POCO ViewModel을 감싸는 Context.
    /// </summary>
    public sealed class NotifyPropertyChangedViewModelContext : ViewModelContext
    {
        readonly INotifyPropertyChanged _inpc;

        readonly Dictionary<string, List<Action<object>>> _listeners = new();

        public NotifyPropertyChangedViewModelContext(INotifyPropertyChanged root)
            : base(root)
        {
            _inpc = root ?? throw new ArgumentNullException(nameof(root));
            _inpc.PropertyChanged += OnPropertyChanged;
        }

        void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 단순 구현: 어떤 프로퍼티가 바뀌든 모든 등록 path를 다시 계산해서 브로드캐스트
            foreach (var kvp in _listeners)
            {
                string path = kvp.Key;
                var callbacks = kvp.Value;
                var value = GetValue(path);

                foreach (var cb in callbacks)
                    cb(value);
            }
        }

        public override object RegisterListener(string path, Action<object> onValueChanged)
        {
            if (string.IsNullOrEmpty(path) || onValueChanged == null)
                return null;

            if (!_listeners.TryGetValue(path, out var list))
            {
                list = new List<Action<object>>();
                _listeners[path] = list;
            }

            list.Add(onValueChanged);

            // 등록 시 현재값 한 번
            onValueChanged(GetValue(path));

            return null; // 따로 토큰 없음
        }

        public override void RemoveListener(string path, Action<object> onValueChanged, object token = null)
        {
            if (string.IsNullOrEmpty(path) || onValueChanged == null)
                return;

            if (!_listeners.TryGetValue(path, out var list))
                return;

            list.Remove(onValueChanged);
            if (list.Count == 0)
                _listeners.Remove(path);
        }
    }
}
