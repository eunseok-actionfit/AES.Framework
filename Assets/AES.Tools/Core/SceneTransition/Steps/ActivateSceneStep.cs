using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class ActivateSceneStep : ITransitionStep
{
    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
        => ctx.Loader.ActivateAsync(ctx.DestinationHandle, ct);
}