using System;
using System.Collections.Generic;
using AES.Tools.TBC.Result;


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
        new T Value { get; set; }

        event Action<T> OnValueChanged;

        /// <summary>이전 값, 현재 값</summary>
        event Action<T, T> OnValueChangedWithPrev;
    }

    /// <summary>
    /// 값 변경 알림 + 선택적 Validation 을 제공하는 기본 구현체.
    /// </summary>
    public class ObservableProperty<T> : IObservableProperty<T>
    {
        private T _value;
        private readonly IEqualityComparer<T> _comparer;

        // --- Validation ---
        private Func<T, Maybe<Error>> _validator;
        private Maybe<Error> _validationError;

        // --- Events ---
        public event Action<T> OnValueChanged = delegate { };
        public event Action<T, T> OnValueChangedWithPrev = delegate { };
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

                var prev = _value;
                _value = value;

                OnValueChanged(_value);            
                OnValueChangedWithPrev(prev, _value);      // 신규 이벤트
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
            T cast;

            if (value is T v) { cast = v; }
            else if (value == null && default(T) == null) { cast = default; }
            else
            {
                string targetName = TypeNameUtil.GetFriendlyTypeName(typeof(T));
                string actualName = value == null
                    ? "null"
                    : TypeNameUtil.GetFriendlyTypeName(value.GetType());

                throw new InvalidCastException(
                    $"값을 타입 {targetName} 으로 캐스팅할 수 없습니다. (실제 타입: {actualName})");
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

        private void Validate()
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

        public IDisposable Subscribe(Action<T, T> action,  bool refresh = true)
        {
            OnValueChangedWithPrev += action;
            
            if (refresh)
                action.Invoke(_value, _value);

            return new Subscription(() => { OnValueChangedWithPrev -= action; });
        }
        
        public IDisposable Subscribe(Action<T> action,  bool refresh = true)
        {
            OnValueChanged += action;
            if (refresh)
                action.Invoke(_value);

            return new Subscription(() => { OnValueChanged -= action; });
        }

        /// <summary>
        /// 값 변경 없이 현재 값을 다시 알린다.
        /// (이벤트성 재발행용)
        /// </summary>
        public void Notify()
        {
            OnValueChanged(_value);
            OnValueChangedWithPrev(_value, _value);
            OnValueChangedBoxed(_value);
        }
    }
}