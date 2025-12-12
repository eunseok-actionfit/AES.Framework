using System;


namespace AES.Tools
{
    public sealed class ObservableBoolPredicate : IPredicate
    {
        private readonly IObservableProperty<bool> prop;
        private readonly bool expected;

        public ObservableBoolPredicate(IObservableProperty<bool> prop, bool expected)
        {
            this.prop = prop ?? throw new ArgumentNullException(nameof(prop));
            this.expected = expected;
        }

        public bool Evaluate() => prop.Value == expected;
    }

    /// <summary>
    /// IObservableProperty&lt;T&gt;의 현재 값을 기대값과 비교한다.
    /// </summary>
    public sealed class ObservableEqualsPredicate<T> : IPredicate
    {
        private readonly IObservableProperty<T> prop;
        private readonly T expected;
        private readonly Func<T, T, bool> equals;

        public ObservableEqualsPredicate(
            IObservableProperty<T> prop,
            T expected,
            Func<T, T, bool> equals = null)
        {
            this.prop = prop ?? throw new ArgumentNullException(nameof(prop));
            this.expected = expected;
            this.equals = equals ?? ((a, b) => Equals(a, b));
        }

        public bool Evaluate() => equals(prop.Value, expected);
    }


    public static partial class Predicates
    {
        // bool 전용
        public static IPredicate IsTrue(this IObservableProperty<bool> p)
            => new ObservableBoolPredicate(p, true);

        public static IPredicate IsFalse(this IObservableProperty<bool> p)
            => new ObservableBoolPredicate(p, false);

        public static IPredicate Is(this IObservableProperty<bool> p, bool expected)
            => new ObservableBoolPredicate(p, expected);

        // 임의 타입 값 비교
        public static IPredicate Is<T>(this IObservableProperty<T> p, T expected)
            => new ObservableEqualsPredicate<T>(p, expected);

        public static IPredicate Is<T>(
            this IObservableProperty<T> p,
            T expected,
            Func<T, T, bool> equals)
            => new ObservableEqualsPredicate<T>(p, expected, equals);
    }
}