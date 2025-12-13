using System;


namespace AES.Tools.StateMachine.Core
{

    /// <summary>
    /// <see cref="StateMachine"/>의 상태 전이를 선언적으로 구성하기 위한 빌더.<br/>
    /// 체이닝 DSL을 통해 가독성 높은 전이 정의를 제공한다.
    /// </summary>
    /// <remarks>
    /// <para><b>기본 사용 예</b></para>
    /// <code>
    /// var idle = new IdleState();
    /// var move = new MoveState();
    /// var dead = new DeadState();
    ///
    /// var isGrounded = new BoolParameter(true);
    /// var moveTrigger = new TriggerParameter();
    /// var isDead = new BoolParameter(false);
    ///
    /// var sm = new StateMachine();
    /// sm.SetState(idle);
    ///
    /// var g = new StateGraphBuilder(sm);
    ///
    /// g.From(idle)
    ///  .To(move)
    ///  .When(isGrounded.IsTrue().And(moveTrigger.AsTrigger()))
    ///  .Priority(10)
    ///  .Named("Idle-&gt;Move")
    ///  .Add();
    ///
    /// g.FromAny()
    ///  .To(dead)
    ///  .When(isDead.IsTrue())
    ///  .Priority(100)
    ///  .Named("Any-&gt;Dead")
    ///  .Add();
    /// </code>
    ///
    /// <para>
    /// 내부적으로는 <see cref="StateMachine.AddTransition{T}"/> /
    /// <see cref="StateMachine.AddAnyTransition{T}"/> 를 호출한다.
    /// </para>
    /// </remarks>
    public sealed class StateGraphBuilder
    {
        private readonly StateMachine machine;
        public StateGraphBuilder(StateMachine machine) => this.machine = machine;

        public FromClause From(IState from) => new FromClause(machine, from);
        public AnyClause FromAny() => new AnyClause(machine);

        public readonly struct FromClause
        {
            private readonly StateMachine m;
            private readonly IState from;

            public FromClause(StateMachine m, IState from)
            {
                this.m = m;
                this.from = from;
            }

            public ToClause To(IState to) => new ToClause(m, from, to);
        }

        public readonly struct AnyClause
        {
            private readonly StateMachine m;
            public AnyClause(StateMachine m) => this.m = m;
            public AnyToClause To(IState to) => new AnyToClause(m, to);
        }

        public readonly struct ToClause
        {
            private readonly StateMachine m;
            private readonly IState from, to;

            public ToClause(StateMachine m, IState from, IState to)
            {
                this.m = m;
                this.from = from;
                this.to = to;
            }

            public TransitionClause When(IPredicate p) => new TransitionClause(m, from, to, p, false);
        }

        public readonly struct AnyToClause
        {
            private readonly StateMachine m;
            private readonly IState to;

            public AnyToClause(StateMachine m, IState to)
            {
                this.m = m;
                this.to = to;
            }

            public TransitionClause When(IPredicate p) => new TransitionClause(m, null, to, p, true);
        }

        public readonly struct TransitionClause
        {
            private readonly StateMachine m;
            private readonly IState from, to;
            private readonly IPredicate p;
            private readonly bool any;
            private readonly int prio;
            private readonly string name;

            public TransitionClause(StateMachine m, IState f, IState t, IPredicate p, bool any, int prio = 0, string name = null)
            {
                this.m = m;
                from = f;
                to = t;
                this.p = p;
                this.any = any;
                this.prio = prio;
                this.name = name;
            }

            public TransitionClause Priority(int p) => new TransitionClause(m, from, to, this.p, any, p, name);
            public TransitionClause Named(string n) => new TransitionClause(m, from, to, p, any, prio, n);

            public void Add()
            {
                if (any) m.AddAnyTransition(to, p, prio, name);
                else m.AddTransition(from, to, p, prio, name);
            }
        }
    }

    /// <summary>
    /// `StateMachine`에 그래프 빌더를 생성하는 확장 메서드 모음.
    /// </summary>
    public static class StateGraphBuilderExtensions
    {
        /// <summary>
        /// 상태 머신 기반의 `StateGraphBuilder`를 생성한다.<br/>
        /// 전이 정의를 간결하게 시작할 수 있다.
        /// </summary>
        /// <param name="machine">그래프를 구성할 상태 머신.</param>
        /// <returns>새로운 상태 그래프 빌더.</returns>
        public static StateGraphBuilder BuildGraph(this StateMachine machine)
            => new StateGraphBuilder(machine);

        // From(...).To(...).When(Func<bool>)
        public static StateGraphBuilder.TransitionClause When(
            this StateGraphBuilder.ToClause clause,
            Func<bool> predicate)
        {
            return clause.When(predicate.ToPredicate());
        }

        // FromAny().To(...).When(Func<bool>)
        public static StateGraphBuilder.TransitionClause When(
            this StateGraphBuilder.AnyToClause clause,
            Func<bool> predicate)
        {
            return clause.When(predicate.ToPredicate());
        }
    }
}