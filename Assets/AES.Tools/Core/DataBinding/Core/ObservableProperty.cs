using System;
using System.Collections.Generic;

namespace AES.Tools
{
    /// <summary>
    /// 단일 속성의 Validation 상태를 노출하는 인터페이스.
    /// Error.None 이면 에러 없음.
    /// </summary>
    public interface IValidatableProperty
    {
        bool HasError { get; }
        Error ValidationError { get; }

        event Action<IValidatableProperty> OnValidationChanged;
    }

    /// <summary>
    /// 박싱된 값/이벤트만 노출하는 비제네릭 ObservableProperty 계약.
    /// 바인딩 계층에서 공통으로 사용한다.
    /// </summary>
    public interface IObservableProperty
    {
        /// <summary>값이 변경될 때 박싱된 값으로 호출</summary>
        event Action<object> OnValueChangedBoxed;

        /// <summary>현재 값 (boxed)</summary>
        object Value { get; }

        /// <summary>실제 값 타입</summary>
        Type ValueType { get; }

        /// <summary>박싱된 값을 세팅 (바인딩에서 사용)</summary>
        void SetBoxedValue(object value);
    }

    /// <summary>
    /// 제네릭 버전 ObservableProperty.
    /// IObservableProperty + IValidatableProperty 를 함께 구현한다.
    /// </summary>
    public interface IObservableProperty<T> : IObservableProperty, IValidatableProperty
    {
        /// <summary>현재 값 (strongly typed)</summary>
        new T Value { get; set; }

        /// <summary>값이 변경될 때 호출 (strongly typed)</summary>
        event Action<T> OnValueChanged;
    }

    /// <summary>
    /// 값 변경 알림 + 선택적 Validation 을 제공하는 기본 구현체.
    /// </summary>
    public class ObservableProperty<T> : IObservableProperty<T>
    {
        T _value;
        readonly IEqualityComparer<T> _comparer;

        // --- Validation ---
        Func<T, Maybe<Error>> _validator;
        Maybe<Error> _validationError;

        // --- Events ---
        public event Action<T> OnValueChanged = delegate { };
        public event Action<object> OnValueChangedBoxed = delegate { };
        public event Action<IValidatableProperty> OnValidationChanged = delegate { };

        public ObservableProperty(
            T initialValue = default,
            Func<T, Maybe<Error>> validator = null,
            IEqualityComparer<T> comparer = null)
        {
            _value = initialValue;
            _validator = validator;
            _comparer = comparer ?? EqualityComparer<T>.Default;

            if (_validator != null)
                Validate();
        }

        // IObservableProperty<T>
        public T Value
        {
            get => _value;
            set
            {
                if (_comparer.Equals(_value, value))
                    return;

                _value = value;
                OnValueChanged(_value);
                OnValueChangedBoxed(_value);

                if (_validator != null)
                    Validate();
            }
        }

        // IObservableProperty (boxed)
        object IObservableProperty.Value => _value;

        public Type ValueType => typeof(T);

        public void SetBoxedValue(object value)
        {
            // null + 값형 케이스 등은 바인딩 쪽에서 사전에 막아두거나,
            // 여기서 기본값으로 떨어뜨리는 식으로 처리.
            T cast;
            if (value is T v)
            {
                cast = v;
            }
            else if (value == null && default(T) == null)
            {
                // 참조형 T 에 null 허용
                cast = default;
            }
            else
            {
                // 필요하면 더 친절한 예외/로그로 교체
                throw new InvalidCastException(
                    $"값을 타입 {typeof(T).Name} 으로 캐스팅할 수 없습니다. (실제 타입: {value?.GetType().Name ?? "null"})");
            }

            Value = cast;
        }

        // IValidatableProperty
        public bool HasError => _validationError.HasValue;

        public Error ValidationError =>
            _validationError.HasValue ? _validationError.Value : Error.None;

        /// <summary>
        /// Validator 교체 또는 신규 설정.
        /// </summary>
        public void SetValidator(Func<T, Maybe<Error>> validator, bool revalidate = true)
        {
            _validator = validator;

            if (revalidate && _validator != null)
                Validate();
        }

        void Validate()
        {
            var newResult = _validator != null
                ? _validator(_value)
                : Maybe<Error>.None;

            var old = _validationError;
            _validationError = newResult;

            bool changed =
                old.HasValue != newResult.HasValue ||
                (old.HasValue && !Equals(old.Value, newResult.Value));

            if (changed)
                OnValidationChanged(this);
        }
    }
}
