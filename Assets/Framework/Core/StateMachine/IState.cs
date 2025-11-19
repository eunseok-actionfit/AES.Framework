
    namespace Framework.Runtime.Core.FiniteStateMachine
    {
        public interface IState {
            void Update() { }
            void FixedUpdate() { }
            void OnEnter() { }
            void OnExit() { }
        }
    }
