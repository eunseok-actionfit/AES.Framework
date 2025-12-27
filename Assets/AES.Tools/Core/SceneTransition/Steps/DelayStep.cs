using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class DelayStep : ITransitionStep
{
    private readonly float _seconds;
    private readonly TransitionStatus _emit;

    public DelayStep(float? seconds, TransitionStatus emit)
    {
        _seconds = seconds ?? 0f;
        _emit = emit;
    }

    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        if (_seconds <= 0f) return UniTask.CompletedTask;

        ctx.Request.Events?.Emit(_emit);
        return UniTask.Delay((int)(_seconds * 1000f), ignoreTimeScale: true, cancellationToken: ct);
    }
}