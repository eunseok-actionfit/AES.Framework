using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class AntiSpillFlushStep : ITransitionStep
{
    private readonly AntiSpill _antiSpill;
    private readonly bool _unloadSpillScene;

    public AntiSpillFlushStep(AntiSpill antiSpill, bool unloadSpillScene = true)
    {
        _antiSpill = antiSpill;
        _unloadSpillScene = unloadSpillScene;
    }

    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
        => _antiSpill == null
            ? UniTask.CompletedTask
            : _antiSpill.FlushToAsync(ctx.DestinationHandle.Scene, _unloadSpillScene, ct);
}