
    namespace Framework.Runtime.Core.FiniteStateMachine
    {
        public interface ITransition {
            IState To { get; }
            IPredicate Condition { get; }
        }
    }
