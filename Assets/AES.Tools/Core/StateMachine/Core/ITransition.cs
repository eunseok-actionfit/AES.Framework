namespace AES.Tools.StateMachine.Core
{
    public interface ITransition {
        IState To { get; }
        IPredicate Condition { get; }
    }
}