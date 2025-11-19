using System;
using System.ComponentModel;
using System.Threading;
using UnityUtils.Observables;


namespace UnityUtils.Bindable
{

    /// <summary>
    /// 데이터 바인딩을 위한 클래스. 단방향 및 양방향 바인딩을 제공하며, 값 변경 시 알림 이벤트를 발생시킵니다.
    /// </summary>
    /// <typeparam name="T">바인딩 데이터의 타입</typeparam>
    public sealed class Bindable<T> : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// `Observable<T>`의 인스턴스를 저장하며 바인딩된 데이터(Source)를 나타냄.
        /// </summary>
        private Observable<T> _src; // 항상 여기에만 구독
        /// Bindable<T>에서 바인딩된 데이터 소스의 쓰기 가능 여부를 나타내는 필드입니다.
        /// true면 쓰기가 가능한 상태, false면 읽기 전용 상태임을 의미합니다.
        private bool _canWrite; // RW 소스인지 표시
        /// <summary>
        /// 비동기 작업의 실행 컨텍스트 정보를 저장하는 변수.
        /// </summary>
        private SynchronizationContext _ctx;

        /// <summary>
        /// 원본 Observable의 이벤트를 해제하기 위한 IDisposable 핸들입니다.
        /// </summary>
        private IDisposable _upstream; // 원본 이벤트 해제용 핸들

        // 상태
        /// <summary>
        /// 객체가 이미 해제(disposed)되었는지 여부를 나타내는 상태 변수입니다.
        /// </summary>
        private bool _disposed;
        /// <summary>
        /// 소스에서 UI로의 알림 진행 여부를 나타내는 플래그.
        /// 알림 재진입을 방지하기 위해 사용됨.
        /// </summary>
        private bool _raising; // 소스→UI 알림 중
        /// <summary>
        /// UI에서 소스로의 데이터 반영 상태를 나타내는 플래그.
        /// true일 경우, UI 변경이 소스에 반영 중임을 의미.
        /// </summary>
        private bool _pushingBack; // UI→소스 반영 중
        /// <summary>
        /// 가장 최근 저장된 값. Observable<T>나 내부 값의 상태를 나타냄.
        /// </summary>
        private T _last;

        /// <summary>바인딩 가능한 데이터 래퍼</summary>
        public Bindable(T initialValue = default, bool canWrite = false, SynchronizationContext ctx = null)
        {
            _ctx = ctx ?? SynchronizationContext.Current;
            _last = initialValue;
            _canWrite = canWrite;
        }

        /// <summary>
        /// 바인딩 상태를 나타냄.
        /// _src가 null이 아니면 true.
        /// </summary>
        public bool IsBound => _src != null;

        /// <summary>
        /// 바인딩된 데이터에 쓰기가 가능한지 여부를 나타냅니다.
        /// </summary>
        public bool CanWrite => _canWrite;

        /// 바인드된 데이터의 현재 값을 가져오거나 설정.
        /// 설정 시 읽기 전용 바인딩이거나 Bind가 되지 않은 경우 예외 발생.
        /// 값 변경 시 PropertyChanged 이벤트 발생.
        /// 소스(Observable)와 동기화된 상태 여부에 따라 값 가져오기 동작이 달라짐.
        public T Value
        {
            get => _src != null ? _src.Value : _last;
            set
            {
                if (!_canWrite)
                    throw new InvalidOperationException("읽기 전용입니다.");
                if (_raising) return;

                if (_src == null)
                {
                    var before = _last;
                    if (!Equals(before, value))
                    {
                        _last = value;
                        RaiseChanged(before, value);
                    }
                    return;
                }

                var beforeSrc = _last;
                try
                {
                    _pushingBack = true;
                    _src.Value = value;            
                    var cur = _src.Value;
                    if (!Equals(beforeSrc, cur)) RaiseChanged(beforeSrc, cur);
                }
                finally { _pushingBack = false; }
            }
        }

        /// <summary>
        /// 값 변경 시 발생하는 이벤트. 이전 값과 새 값을 전달합니다.
        /// </summary>
        public event Action<T, T> ValueChanged;

        /// <summary>
        /// INotifyPropertyChanged 인터페이스에서 제공되는 PropertyChanged 이벤트입니다.
        /// 객체의 속성 값이 변경되었음을 알리는 데 사용됩니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        // ──────────────────────────────────────────────────────────────
        // 바인딩 API
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 읽기 전용(IReadOnlyObservable) 단방향 바인딩을 설정합니다.
        /// </summary>
        /// <param name="source">바인딩할 읽기 전용 데이터 소스입니다.</param>
        /// <param name="ctx">동기화 컨텍스트입니다. (옵션)</param>
        /// <returns>바인딩 해제를 위한 IDisposable 핸들입니다.</returns>
        public IDisposable Bind(IReadOnlyObservable<T> source, SynchronizationContext ctx = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            RebindRO(source, ctx);
            return new Scope(UnBind);
        }

        /// <summary>쓰기 가능(Observable) 양방향 바인딩</summary>
        /// <param name="source">바인딩할 Observable 데이터 소스</param>
        /// <param name="ctx">동기화 컨텍스트 (optional)</param>
        /// <returns>바인딩 해제를 위한 IDisposable 객체</returns>
        public IDisposable Bind(Observable<T> source, SynchronizationContext ctx = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            RebindRW(source, ctx);
            return new Scope(UnBind);
        }

        /// <summary>현재 소스와의 연결을 해제합니다.</summary>
        public void UnBind()
        {
            if (_disposed) return;

            // 해지 핸들만 Dispose -> 내부에서 원본 이벤트 -={} 실행
            _upstream?.Dispose();
            _upstream = null;

            if (_src != null)
            {
                _src.OnValueChangedFromTo -= OnSrcChangedPrev;
                _src.OnValueChangedTo -= OnSrcChangedTo;
                _src.OnValueChanged -= OnSrcChanged;
                _src = null;
            }

            _canWrite = false;
        }

        /// <summary>값 강제 재발행(강제로 값 변화를 알림)</summary>
        /// <param name="force">값이 동일하더라도 강제 알림 여부</param>
        public void Refresh(bool force = false)
        {
            var cur = _src != null ? _src.Value : _last;
            if (force) RaiseChanged(_last, cur);
            else OnSrcChangedTo(cur);
        }

        // ──────────────────────────────────────────────────────────────
        // 내부
        // ──────────────────────────────────────────────────────────────

        /// <summary>읽기/쓰기 가능한 Observable 재바인딩</summary>
        /// <param name="rw">읽기/쓰기 가능한 Observable</param>
        /// <param name="ctx">동기화 컨텍스트</param>
        private void RebindRW(Observable<T> rw, SynchronizationContext ctx)
        {
            UnBind();
            _ctx = ctx ?? _ctx;
            _src = rw;
            _canWrite = true;

            _last = _src.Value;
            _src.OnValueChangedFromTo += OnSrcChangedPrev;
            _src.OnValueChangedTo += OnSrcChangedTo;
            _src.OnValueChanged += OnSrcChanged;

            // 초기 push
            RaiseChanged(_last, _last);
        }

        /// <summary>읽기 전용(IReadOnlyObservable) 재바인딩</summary>
        /// <param name="ro">읽기 전용 소스</param>
        /// <param name="ctx">동기화 컨텍스트</param>
        private void RebindRO(IReadOnlyObservable<T> ro, SynchronizationContext ctx)
        {
            UnBind();
            _ctx = ctx ?? _ctx;
            _canWrite = false;

            if (ro is Observable<T> obs)
            {
                // 같은 구현이면 그냥 그 observable 하나만 소스로 사용
                _src = obs;
                _last = _src.Value;
                _src.OnValueChangedFromTo += OnSrcChangedPrev;
                _src.OnValueChangedTo += OnSrcChangedTo;
                _src.OnValueChanged += OnSrcChanged;
                RaiseChanged(_last, _last);
                return;
            }

            // 다른 구현이면 feeder Observable로 단일화
            _src = new Observable<T>(ro.Value);
            _last = _src.Value;

            _src.OnValueChangedFromTo += OnSrcChangedPrev;
            _src.OnValueChangedTo += OnSrcChangedTo;
            _src.OnValueChanged += OnSrcChanged;

            //  원본 이벤트 구독 + 해지 핸들을 IDisposable로 구성해서 필드에는 "핸들"만 저장
            Action<T, T> hPrev = (_, newV) => _src.Value = newV;
            Action<T> hNew = (newV) => _src.Value = newV;

            ro.OnValueChangedFromTo += hPrev;
            ro.OnValueChangedTo += hNew;

            _upstream = new Scope(() => {
                try { ro.OnValueChangedFromTo -= hPrev; }
                catch
                { // ignored
                }

                try { ro.OnValueChangedTo -= hNew; }
                catch
                { // ignored
                }
            });

            RaiseChanged(_last, _last);
        }
        private void OnSrcChanged()
        {
            if (_disposed) return;
            if (_pushingBack) return;

            RaiseChanged(_last, _last);
        }

        /// <summary>소스 값 변경 시 호출되는 핸들러</summary>
        /// <param name="newV">변경된 새 값</param>
        private void OnSrcChangedTo(T newV)
        {
            if (_disposed) return;
            if (_pushingBack) return;

            if (!Equals(_last, newV)) RaiseChanged(_last, newV);
        }

        /// <summary>이전 값(oldV)과 새로운 값(newV)을 다룰 때 호출되는 핸들러</summary>
        /// <param name="oldV">이전 값</param>
        /// <param name="newV">새로운 값</param>
        private void OnSrcChangedPrev(T oldV, T newV)
        {
            if (_disposed) return;
            if (_pushingBack) return;

            if (!Equals(oldV, newV)) RaiseChanged(oldV, newV);
        }

        /// <summary>ValueChanged 및 PropertyChanged 이벤트를 발생</summary>
        /// <param name="oldV">변경 전 값</param>
        /// <param name="newV">변경 후 값</param>
        private void RaiseChanged(T oldV, T newV)
        {
            if (_disposed) return;
            if (_raising) return;

            _last = newV;
            _raising = true;
            try
            {
                void Fire()
                {
                    ValueChanged?.Invoke(oldV, newV);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
                if (_ctx != null) _ctx.Post(_ => Fire(), null);
                else Fire();
            }
            finally { _raising = false; }
        }

        /// <summary>Bindable 객체의 자원을 해제하는 메서드</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnBind();
        }

        /// <summary>
        /// 한정된 작업 범위를 나타내는 객체를 제공합니다.
        /// </summary>
        private sealed class Scope : IDisposable
        {
            /// <summary>
            /// Scope 클래스에서 Dispose 시 호출될 작업을 나타냄.
            /// </summary>
            private Action _d;

            /// <summary>자원 해제를 지원하는 실행 컨텍스트</summary>
            public Scope(Action d) => _d = d;

            /// <summary>바인딩 해제 및 리소스 해제</summary>
            public void Dispose()
            {
                _d?.Invoke();
                _d = null;
            }
        }
    }
}