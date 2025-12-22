using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class BlockInputStep : ITransitionStep
{
    private readonly IInputBlocker _blocker;
    public BlockInputStep(IInputBlocker blocker) => _blocker = blocker;

    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        _blocker?.Block();
        ctx.Request.Events?.Emit(TransitionStatus.InputBlocked);
        return UniTask.CompletedTask;
    }
}