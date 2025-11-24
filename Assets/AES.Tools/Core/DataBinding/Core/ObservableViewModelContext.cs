using System;
using System.Collections.Generic;

namespace AES.Tools
{
    /// <summary>
    /// ObservableProperty / ObservableList 기반 ViewModel에 최적화된 Context.
    /// </summary>
    public sealed class ObservableViewModelContext : ViewModelContext
    {
        private class ListenerEntry
        {
            public Action<object> Callback;
            public object Source;         // IObservableProperty / IObservableList
            public Delegate Subscription; // 해제용 델리게이트
        }

        private readonly Dictionary<string, List<ListenerEntry>> _listeners = new();

        public ObservableViewModelContext(object root) : base(root) { }

        public override object RegisterListener(string path, Action<object> onValueChanged)
        {
            if (string.IsNullOrEmpty(path) || onValueChanged == null)
                return null;

            var mp = GetMemberPath(path);
            var value = mp.GetValue(Root);

            // ObservableProperty
            if (value is IObservableProperty op)
            {
                Action<object> handler = onValueChanged;
                op.OnValueChangedBoxed += handler;

                AddListener(path, onValueChanged, op, handler);

                onValueChanged(op.Value); // 초기값
                return op;
            }

            // ObservableList
            if (value is IObservableList list)
            {
                Action handler = () => onValueChanged(list);
                list.OnListChanged += handler;

                AddListener(path, onValueChanged, list, handler);

                onValueChanged(list);
                return list;
            }

            // 그 외: 이벤트 없이 값만 한 번
            onValueChanged(value);
            return null;
        }

        public override void RemoveListener(string path, Action<object> onValueChanged, object token = null)
        {
            if (string.IsNullOrEmpty(path) || onValueChanged == null)
                return;

            if (!_listeners.TryGetValue(path, out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var e = list[i];
                if (e.Callback != onValueChanged)
                    continue;

                if (e.Source is IObservableProperty op && e.Subscription is Action<object> h1)
                    op.OnValueChangedBoxed -= h1;
                else if (e.Source is IObservableList ol && e.Subscription is Action h2)
                    ol.OnListChanged -= h2;

                list.RemoveAt(i);
            }

            if (list.Count == 0)
                _listeners.Remove(path);
        }

        private void AddListener(string path, Action<object> cb, object source, Delegate sub)
        {
            if (!_listeners.TryGetValue(path, out var list))
            {
                list = new List<ListenerEntry>();
                _listeners[path] = list;
            }

            list.Add(new ListenerEntry
            {
                Callback = cb,
                Source = source,
                Subscription = sub
            });
        }
        
        public override void SetValue(string path, object value)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var mp = GetMemberPath(path);
            var tokens = mp.Tokens;
            if (tokens == null || tokens.Count == 0)
                return;

            object current = Root;

            // 마지막 토큰 직전까지 내려감
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var t = tokens[i];
                current = t switch
                {
                    MemberToken m => GetMember(current, m.Name),
                    IndexToken idx => GetIndex(current, idx.Index),
                    KeyToken key => GetKey(current, key.Key),
                    _ => current
                };

                if (current == null)
                    return;
            }

            var last = tokens[^1];

            if (last is MemberToken lm)
            {
                // 마지막 멤버가 IObservableProperty인지 먼저 체크
                var target = GetMember(current, lm.Name);

                if (target is IObservableProperty op && value is not IObservableProperty)
                {
                    // ObservableProperty<T>면 객체를 교체하지 않고 내부 값만 설정
                    op.SetBoxedValue(value);
                    return;
                }

                //  그 외는 기존 로직대로 일반 멤버 세팅
                SetMember(current, lm.Name, value);
            }

            // todo 인덱서/딕셔너리 셋터가 필요하면 여기서 확장
        }

    }
}
