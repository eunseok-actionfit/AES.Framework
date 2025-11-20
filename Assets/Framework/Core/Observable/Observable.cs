using System;
using System.Collections.Generic;


namespace AES.Tools { // 외부에 내보낼 읽기 전용 인터페이스
    /// <summary>
    /// 읽기 전용으로 관찰 가능한 데이터 인터페이스.
    /// 값 변경 시 구독자에게 알림을 제공하며 이벤트를 통해 현재 상태를 수신 가능.
    /// </summary>
    /// <typeparam name="T">
    /// 관찰 대상의 데이터 타입.
    /// </typeparam>
    public interface IReadOnlyObservable<T>
    {
        /// <summary>
        /// 현재 값을 나타내는 속성입니다.
        /// 읽기 전용으로 접근 가능하며 값 변경 시 관련 이벤트가 트리거됩니다.
        /// </summary>
        T Value { get; }
        
        event Action OnValueChanged;

        /// <summary>
        /// 값이 변경될 때 이벤트를 트리거하는 대리자입니다.
        /// 구독자에게 새로 설정된 값을 제공합니다.
        /// </summary>
        event Action<T> OnValueChangedTo; // 새 값만

        /// <summary>
        /// 이전 값과 새 값을 포함하여 값 변경 이벤트를 나타냅니다.
        /// </summary>
        event Action<T, T> OnValueChangedFromTo; // (옵션) 이전 값 포함

        /// <summary>
        /// 현재 값과 이전 값을 기반으로 값 변경 이벤트를 강제로 재호출합니다.
        /// 이벤트 초기화 또는 상태 갱신을 위해 사용됩니다.
        /// </summary>
        void OnValueChangedToRefresh();
    }


    /// <summary>
    /// 관찰 가능한 데이터의 래퍼 클래스.
    /// 값 변경을 이벤트로 알리고, 구독자를 통해 변경 사항을 배포.
    /// </summary>
    /// <typeparam name="T">
    /// 관찰 값의 데이터 타입.
    /// </typeparam>
    public class Observable<T> : IReadOnlyObservable<T>
    {
        public event Action OnValueChanged = delegate { };
        /// <summary>
        /// 값이 변경될 때 트리거 되는 이벤트입니다.
        /// 변경된 새로운 값이 이벤트 데이터로 전달됩니다.
        /// </summary>
        public event Action<T> OnValueChangedTo = delegate { };

        /// <summary>
        /// 이전 값과 새 값이 전달되는 값 변경 이벤트입니다.
        /// </summary>
        public event Action<T, T> OnValueChangedFromTo = delegate { };

        /// <summary>
        /// 객체의 현재 값을 저장하는 필드입니다.
        /// <see cref="Value"/> 프로퍼티를 통해 접근되며, 값 변경 시 관련 이벤트가 트리거됩니다.
        /// </summary>
        private T _currentValue;
        /// <summary>
        /// 이전 값을 저장하는 변수입니다.
        /// 값 변경 시 기존 값이 이 변수로 갱신됩니다.
        /// </summary>
        private T _previousValue;

        /// <summary>
        /// 현재 값을 나타내는 속성입니다.
        /// 값 변경 시 이벤트가 발생하며, 이전 값과 새 값을 포함한 알림도 제공합니다.
        /// </summary>
        public T Value
        {
            get => _currentValue;
            set
            {
                var newValue = OnValueChanging(_currentValue, value);
                if (!EqualityComparer<T>.Default.Equals(_currentValue, newValue))
                {
                    _previousValue = _currentValue;
                    _currentValue = newValue;

                    OnValueChangedToRefresh();
                }
            }
        }

        /// <summary>
        /// 값 변경을 관찰 가능하게 하는 객체로, <typeparamref name="T"/> 타입의 값 변경 이벤트를 제공합니다.
        /// </summary>
        public Observable(T initialValue = default) => _currentValue = initialValue;

        /// <summary>
        /// 현재 값과 이전 값을 기반으로 OnValueChangedTo 및 OnValueChangedFromTo 이벤트를 강제로 호출합니다.
        /// 구독자들에게 상태 업데이트를 알리고 변경 여부와 무관하게 최신 상태를 다시 전달합니다.
        /// </summary>
        public void OnValueChangedToRefresh()
        {
            OnValueChanged.Invoke();
            OnValueChangedTo.Invoke(_currentValue);
            OnValueChangedFromTo.Invoke(_previousValue, _currentValue);
        }

        /// <summary>
        /// 값이 변경되기 전에 호출되는 메서드로, 새로운 값을 설정하기 전에 값의 변경을 가로챌 수 있습니다.
        /// 새로운 값을 검증하거나, 수정할 수 있는 로직을 추가할 수 있습니다.
        /// </summary>
        /// <param name="prev">이전 값</param>
        /// <param name="next">새로 설정될 값</param>
        /// <returns>최종적으로 설정할 값</returns>
        protected virtual T OnValueChanging(T prev, T next) => next;

        /// <summary>
        /// Observable 객체를 IReadOnlyObservable 형태로 제공합니다.
        /// 이 메서드는 데이터를 외부에서 변경하지 못하도록 읽기 전용으로 변환합니다.
        /// </summary>
        /// <returns>
        /// IReadOnlyObservable 형태의 읽기 전용 Observable 객체를 반환합니다.
        /// </returns>
        public IReadOnlyObservable<T> AsReadOnly() => this;
    }
}