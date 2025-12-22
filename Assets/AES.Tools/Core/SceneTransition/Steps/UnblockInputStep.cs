using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class UnblockInputStep : ITransitionStep
{
    private readonly IInputBlocker _blocker;
    public UnblockInputStep(IInputBlocker blocker) => _blocker = blocker;

    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        _blocker?.Unblock();
        ctx.Request.Events?.Emit(TransitionStatus.InputUnblocked);
        return UniTask.CompletedTask;
    }
}