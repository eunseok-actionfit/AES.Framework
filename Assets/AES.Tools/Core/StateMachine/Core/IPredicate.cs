using System;


namespace AES.Tools.StateMachine.Core
{
    public interface IPredicate
    {
        bool Evaluate();
    }

    public sealed class DelegatePredicate : IPredicate
    {
        private readonly Func<bool> _func;
        public DelegatePredicate(Func<bool> func)
            => _func = func ?? throw new ArgumentNullException(nameof(func));

        public bool Evaluate() => _func();
    }

    public sealed class AndPredicate : IPredicate
    {
        private readonly IPredicate _a;
        private readonly IPredicate _b;

        public AndPredicate(IPredicate a, IPredicate b)
        {
            _a = a;
            _b = b;
        }

        public bool Evaluate() => _a.Evaluate() && _b.Evaluate();
    }

    public sealed class OrPredicate : IPredicate
    {
        private readonly IPredicate _a;
        private readonly IPredicate _b;

        public OrPredicate(IPredicate a, IPredicate b)
        {
            _a = a;
            _b = b;
        }

        public bool Evaluate() => _a.Evaluate() || _b.Evaluate();
    }

    public sealed class NotPredicate : IPredicate
    {
        private readonly IPredicate _inner;
        public NotPredicate(IPredicate inner) => _inner = inner;
        public bool Evaluate() => !_inner.Evaluate();
    }

    // 이벤트 기반 1회성 조건
    public sealed class ActionPredicate : IPredicate
    {
        public bool flag;

        public ActionPredicate(ref Action evt)
            => evt += () => flag = true;

        public bool Evaluate()
        {
            bool r = flag;
            flag = false;
            return r;
        }
    }
}