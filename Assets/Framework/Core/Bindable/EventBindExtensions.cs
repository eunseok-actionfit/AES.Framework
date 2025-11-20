using System;


// Observable<T>

namespace AES.Tools
{
    public static class EventBindExtensions
    {
        /// <summary>
        /// Action&lt;TSrc&gt; 이벤트를 Bindable&lt;T&gt;에 바인딩.
        /// - add/remove: 이벤트 구독/해지 람다 (예: h => obj.Evt += h / h => obj.Evt -= h)
        /// - map: TSrc → T 변환 (동일 타입이면 null 가능)
        /// - initial: 바인딩 직후 한 번 흘려 보낼 초기 값(옵션)
        /// 반환: IDisposable (해지 시 이벤트 해제 + 내부 바인딩 해제)
        /// </summary>
        public static IDisposable Bind<T, TSrc>(
            this Bindable<T> bindable,
            Action<Action<TSrc>> add,
            Action<Action<TSrc>> remove,
            Func<TSrc, T> map = null,
            T initial = default)
        {
            if (bindable == null) throw new ArgumentNullException(nameof(bindable));
            if (add == null)      throw new ArgumentNullException(nameof(add));
            if (remove == null)   throw new ArgumentNullException(nameof(remove));
            map ??= (TSrc v) => (v is T t) ? t : throw new InvalidCastException(
                $"Cannot map {typeof(TSrc).Name} to {typeof(T).Name}. Provide a map.");

            // 이벤트 → 로컬 피더 Observable
            var feeder = new Observable<T>(initial);
            var inner  = bindable.Bind(feeder); // 기존 Bind(Observable<T>) 재사용

            Action<TSrc> handler = v => feeder.Value = map(v);
            add(handler);

            return new Scope(() =>
            {
                try { remove(handler); } catch { /* no-op */ }
                inner?.Dispose();
            });
        }

        /// <summary>
        /// TSrc==T 인 경우의 축약 오버로드
        /// </summary>
        public static IDisposable Bind<T>(
            this Bindable<T> bindable,
            Action<Action<T>> add,
            Action<Action<T>> remove,
            T initial = default)
            => bindable.Bind<T, T>(add, remove, map: null, initial: initial);

        private sealed class Scope : IDisposable
        {
            private Action _d; public Scope(Action d) => _d = d;
            public void Dispose() { _d?.Invoke(); _d = null; }
        }
    }
}