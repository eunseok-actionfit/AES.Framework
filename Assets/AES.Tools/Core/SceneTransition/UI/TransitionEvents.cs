using System;

public sealed class TransitionEvents : ITransitionEvents
{
    public event Action<TransitionStatus> OnStatus;

    public void Emit(TransitionStatus status)
    {
        OnStatus?.Invoke(status);
    }
}