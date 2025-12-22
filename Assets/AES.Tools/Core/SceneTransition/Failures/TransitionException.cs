public sealed class TransitionException : System.Exception
{
    public readonly TransitionFailCode Code;

    public TransitionException(TransitionFailCode code, string message, System.Exception inner = null)
        : base(message, inner)
    {
        Code = code;
    }
}