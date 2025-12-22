using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class UnloadLoadingScreenStep : ITransitionStep
{
    private readonly CleanupPlan _plan;

    public UnloadLoadingScreenStep(CleanupPlan plan)
    {
        _plan = plan;
    }

    public async UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        if (ctx.LoadingPresenter == null)
            return;

        await ctx.LoadingPresenter.HideAsync(ctx, _plan, ct);
    }
}