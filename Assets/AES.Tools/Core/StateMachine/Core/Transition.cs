using System;

namespace AES.Tools
{
    public abstract class Transition
    {
        public IState To { get; protected set; }

        /// <summary>
        /// 전이 우선순위. 클수록 우선.
        /// 같은 프레임에 여러 전이가 true면 Priority 최대값 하나만 사용.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 디버깅용 이름 (없으면 null).
        /// </summary>
        public string Name { get; set; }

        public abstract bool Evaluate();
    }

    public class Transition<T> : Transition
    {
        public readonly T condition;

        public Transition(IState to, T condition)
        {
            To = to;
            this.condition = condition;
        }

        public override bool Evaluate()
        {
            // Func<bool>
            if (condition is Func<bool> func)
                return func();

            // ActionPredicate
            if (condition is ActionPredicate ap)
                return ap.Evaluate();

            // IPredicate
            if (condition is IPredicate pred)
                return pred.Evaluate();

            // 그 외 타입은 false
            return false;
        }
    }

    /// <summary>
    /// Func<bool> 기반 Predicate.
    /// </summary>
    public class FuncPredicate : IPredicate
    {
        readonly Func<bool> func;

        public FuncPredicate(Func<bool> func)
        {
            this.func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public bool Evaluate() => func.Invoke();
    }

    /// <summary>
    /// Action 기반 Trigger 느낌의 Predicate.
    /// 한 번 true 반환 후 flag를 자동으로 false로 리셋.
    /// </summary>
    public class ActionPredicate : IPredicate
    {
        public bool flag;

        public ActionPredicate(ref Action eventReaction)
            => eventReaction += () => { flag = true; };

        public bool Evaluate()
        {
            bool result = flag;
            flag = false;
            return result;
        }
    }
}