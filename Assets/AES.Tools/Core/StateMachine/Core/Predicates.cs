using System;

namespace AES.Tools
{
    public sealed class DelegatePredicate : IPredicate
    {
        readonly Func<bool> _func;

        public DelegatePredicate(Func<bool> func)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public bool Evaluate() => _func();
    }

    public sealed class AndPredicate : IPredicate
    {
        readonly IPredicate _a;
        readonly IPredicate _b;

        public AndPredicate(IPredicate a, IPredicate b)
        {
            _a = a ?? throw new ArgumentNullException(nameof(a));
            _b = b ?? throw new ArgumentNullException(nameof(b));
        }

        public bool Evaluate() => _a.Evaluate() && _b.Evaluate();
    }

    public sealed class OrPredicate : IPredicate
    {
        readonly IPredicate _a;
        readonly IPredicate _b;

        public OrPredicate(IPredicate a, IPredicate b)
        {
            _a = a ?? throw new ArgumentNullException(nameof(a));
            _b = b ?? throw new ArgumentNullException(nameof(b));
        }

        public bool Evaluate() => _a.Evaluate() || _b.Evaluate();
    }

    public sealed class NotPredicate : IPredicate
    {
        readonly IPredicate _inner;

        public NotPredicate(IPredicate inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool Evaluate() => !_inner.Evaluate();
    }

    public static class Predicates
    {
        public static IPredicate ToPredicate(this Func<bool> func)
            => new DelegatePredicate(func);

        public static IPredicate IsTrue(this BoolParameter param)
            => new BoolParameterPredicate(param, true);

        public static IPredicate IsFalse(this BoolParameter param)
            => new BoolParameterPredicate(param, false);

        public static IPredicate AsTrigger(this TriggerParameter param)
            => new TriggerPredicate(param);

        public static IPredicate And(this IPredicate a, IPredicate b)
            => new AndPredicate(a, b);

        public static IPredicate Or(this IPredicate a, IPredicate b)
            => new OrPredicate(a, b);

        public static IPredicate Not(this IPredicate inner)
            => new NotPredicate(inner);

        public static IPredicate And(this IPredicate a, Func<bool> b)
            => new AndPredicate(a, b.ToPredicate());

        public static IPredicate And(this Func<bool> a, IPredicate b)
            => new AndPredicate(a.ToPredicate(), b);

        public static IPredicate Or(this IPredicate a, Func<bool> b)
            => new OrPredicate(a, b.ToPredicate());

        public static IPredicate Or(this Func<bool> a, IPredicate b)
            => new OrPredicate(a.ToPredicate(), b);
    }
}
