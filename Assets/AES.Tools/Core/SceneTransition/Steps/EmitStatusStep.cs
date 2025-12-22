using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class EmitStatusStep : ITransitionStep
{
    private readonly TransitionStatus _status;
    private readonly ITransitionUI _ui;

    public EmitStatusStep(TransitionStatus status, ITransitionUI ui)
    {
        _status = status;
        _ui = ui;
    }

    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        ctx.Request.Events?.Emit(_status);
        TransitionUIBinder.ApplyStatus(_ui, _status);
        return UniTask.CompletedTask;
    }
}