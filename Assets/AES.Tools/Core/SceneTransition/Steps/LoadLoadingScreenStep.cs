using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class LoadLoadingScreenStep : ITransitionStep
{
    private readonly CleanupPlan _plan;
    private readonly LoadingPresenterFactory _factory;

    public LoadLoadingScreenStep(CleanupPlan plan, LoadingPresenterFactory factory)
    {
        _plan = plan;
        _factory = factory;
    }

    public async UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        if (ctx.LoadingPresenter == null)
            ctx.LoadingPresenter = _factory.Select(ctx.Request);

        if (ctx.LoadingPresenter == null)
            return;

        await ctx.LoadingPresenter.ShowAsync(ctx, _plan, ct);
    }
}   