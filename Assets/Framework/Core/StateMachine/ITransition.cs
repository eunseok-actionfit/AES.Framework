namespace AES.Tools
{
    public interface ITransition {
        IState To { get; }
        IPredicate Condition { get; }
    }
}