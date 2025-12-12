namespace AES.Tools
{
    /// <summary>
    /// Unity 의존성이 없는 상태 머신 소유자.<br/>
    /// 순수 로직 계층에서 사용된다.
    /// </summary>
    /// <remarks>
    /// View와 완전히 분리된 구조를 유지한다.<br/>
    /// 서버 로직이나 테스트 코드에 적합하다.
    /// </remarks>
    public abstract class StatefulObject : IStateMachineOwner
    {
        protected readonly StateMachine stateMachine = new();
        public StateMachine Machine => stateMachine;

        protected void At(IState from, IState to, IPredicate condition, int priority = 0, string name = null)
            => stateMachine.AddTransition(from, to, condition, priority, name);

        protected void Any(IState to, IPredicate condition, int priority = 0, string name = null)
            => stateMachine.AddAnyTransition(to, condition, priority, name);

        public void Update()
            => stateMachine.Update();

        public void FixedUpdate()
            => stateMachine.FixedUpdate();

        protected void SetInitialState(IState state)
            => stateMachine.SetState(state);
    }
}
