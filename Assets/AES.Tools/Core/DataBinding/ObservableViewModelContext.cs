using System;

namespace AES.Tools
{
    /// <summary>
    /// ObservableProperty / ObservableList 기반 ViewModel에 최적화된 Context.
    /// RegisterListener 는 항상 IDisposable 토큰(Subscription)을 리턴한다.
    /// RemoveListener 는 토큰이 IDisposable 이면 Dispose() 만 호출한다.
    /// </summary>
    public sealed class ObservableViewModelContext : ViewModelContext
    {
        public ObservableViewModelContext(object root) : base(root) { }

        public override object RegisterListener(string path, Action<object> onValueChanged)
        {
            if (string.IsNullOrEmpty(path) || onValueChanged == null)
                return null;

            var mp    = GetMemberPath(path);
            var value = mp.GetValue(Root);

            // 1) ObservableProperty
            if (value is IObservableProperty op)
            {
                void Handler(object v) => onValueChanged(v);

                op.OnValueChangedBoxed += Handler;

                // 초기 값 한 번 밀어줌
                onValueChanged(op.Value);

                return new Subscription(() =>
                {
                    op.OnValueChangedBoxed -= Handler;
                });
            }

            // 2) ObservableList
            if (value is IObservableList list)
            {
                void Handler()
                {
                    onValueChanged(list);
                }

                list.OnListChanged += Handler;

                // 초기 값 한 번
                onValueChanged(list);

                return new Subscription(() =>
                {
                    list.OnListChanged -= Handler;
                });
            }

            // 3) 그 외: 이벤트 없이 현재 값만 한 번
            onValueChanged(value);
            return null;
        }

        public override void RemoveListener(string path, Action<object> onValueChanged, object token = null)
        {
            // 권장 구현: path/callback 은 무시하고 토큰만 Dispose
            if (token is IDisposable d)
            {
                d.Dispose();
            }
            // 토큰이 없으면 (구버전 컨텍스트가 아닌 이상) 아무 것도 안 함
        }

        /// <summary>
        /// ObservableProperty 를 고려한 SetValue.
        /// 마지막 멤버가 IObservableProperty 면 객체 교체 대신 내부 값만 설정.
        /// </summary>
        public override void SetValue(string path, object value)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var mp     = GetMemberPath(path);
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
                // 마지막 멤버가 IObservableProperty 인지 먼저 체크
                var target = GetMember(current, lm.Name);

                if (target is IObservableProperty op && value is not IObservableProperty)
                {
                    // ObservableProperty<T> 면 객체를 교체하지 않고 내부 값만 설정
                    op.SetBoxedValue(value);
                    return;
                }

                // 그 외는 기존 로직대로 일반 멤버 세팅
                SetMember(current, lm.Name, value);
            }

            // todo 인덱서/딕셔너리 셋터가 필요하면 여기서 확장
        }
    }
}
