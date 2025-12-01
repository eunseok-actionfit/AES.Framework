using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AES.Tools
{
    /// <summary>
    /// INotifyPropertyChanged 기반 POCO ViewModel을 감싸는 Context.
    /// RegisterListener 는 항상 IDisposable 토큰(Subscription)을 리턴한다.
    /// </summary>
    public sealed class NotifyPropertyChangedViewModelContext : ViewModelContext
    {
        private readonly INotifyPropertyChanged _inpc;

        // path -> callbacks
        private readonly Dictionary<string, List<Action<object>>> _listeners = new();

        public NotifyPropertyChangedViewModelContext(INotifyPropertyChanged root)
            : base(root)
        {
            _inpc = root ?? throw new ArgumentNullException(nameof(root));
            _inpc.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 단순 구현: 어떤 프로퍼티가 바뀌든 모든 등록 path를 다시 계산해서 브로드캐스트
            foreach (var kvp in _listeners)
            {
                string path          = kvp.Key;
                var callbacks        = kvp.Value;
                var valueForThisPath = GetValue(path);

                foreach (var cb in callbacks)
                    cb(valueForThisPath);
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

            // 이 콜백만 제거하는 Subscription 반환
            return new Subscription(() =>
            {
                if (!_listeners.TryGetValue(path, out var inner))
                    return;

                inner.Remove(onValueChanged);
                if (inner.Count == 0)
                    _listeners.Remove(path);
            });
        }

        public override void RemoveListener(string path, Action<object> onValueChanged, object token = null)
        {
            // IDisposable 토큰이 있으면 그걸 Dispose 하는 것이 표준
            if (token is IDisposable d)
            {
                d.Dispose();
                return;
            }

            // 토큰 없는 구버전 호출 대응이 필요하면 여기서 path/cb 기반 제거를 구현할 수도 있지만,
            // 현재 프로젝트에서는 모든 호출이 토큰을 넘기고 있으므로 생략해도 된다.
        }
    }
}
