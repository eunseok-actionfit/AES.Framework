using System;


namespace AES.Tools
{
    /// <summary>
    /// <see cref="IPredicate"/> 생성을 돕는 확장 메서드 모음.<br/>
    /// 논리 조합(AND / OR / NOT)을 간결하게 표현할 수 있다.
    /// </summary>
    /// <remarks>
    /// <para><b>Predicate 조합 예</b></para>
    /// <code>
    /// IPredicate canMove =
    ///     isGrounded.IsTrue()
    ///               .And(moveTrigger.AsTrigger())
    ///               .And(() =&gt; stamina &gt; 0)
    ///               .And(isDead.IsFalse().Not());
    /// </code>
    ///
    /// <para>
    /// 모든 Predicate는 <see cref="IPredicate.Evaluate"/>를 통해 평가된다.
    /// </para>
    /// </remarks>
    public static partial class Predicates
    {
        public static IPredicate ToPredicate(this Func<bool> func)
            => new DelegatePredicate(func);

        public static IPredicate And(this IPredicate a, IPredicate b)
            => new AndPredicate(a, b);

        public static IPredicate Or(this IPredicate a, IPredicate b)
            => new OrPredicate(a, b);

        public static IPredicate Not(this IPredicate inner)
            => new NotPredicate(inner);

        public static IPredicate And(this IPredicate a, Func<bool> b)
            => new AndPredicate(a, b.ToPredicate());

        public static IPredicate Or(this IPredicate a, Func<bool> b)
            => new OrPredicate(a, b.ToPredicate());


        public static IPredicate HasExitTime(this StateMachine sm, float seconds)
            => new DelegatePredicate(() => sm.TimeInState >= seconds);
    }
}