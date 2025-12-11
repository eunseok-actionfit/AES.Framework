namespace AES.Tools
{
    // View와 완전히 분리된 순수 로직 베이스
    public abstract class StatefulObject : IStateMachineOwner
    {
        protected readonly StateMachine stateMachine = new();
        public StateMachine Machine => stateMachine;

        // 기본 전이
        protected void At<T>(IState from, IState to, T condition)
            => stateMachine.AddTransition(from, to, condition);

        protected void Any<T>(IState to, T condition)
            => stateMachine.AddAnyTransition(to, condition);

        // priority 지원 버전
        protected void At<T>(IState from, IState to, T condition, int priority, string name = null)
            => stateMachine.AddTransition(from, to, condition, priority, name);

        protected void Any<T>(IState to, T condition, int priority, string name = null)
            => stateMachine.AddAnyTransition(to, condition, priority, name);

        public void Update() => stateMachine.Update();
        public void FixedUpdate() => stateMachine.FixedUpdate();

        protected void SetInitialState(IState state)
            => stateMachine.SetState(state);
    }
}