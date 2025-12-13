namespace AES.Tools.StateMachine.Core
{
    public interface IStateMachineOwner
    {
        StateMachine Machine { get; }
    }
}