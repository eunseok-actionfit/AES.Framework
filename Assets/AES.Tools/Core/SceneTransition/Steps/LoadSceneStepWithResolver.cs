using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class LoadSceneStepWithResolver : ITransitionStep
{
    private readonly IProgress<float> _progress;
    private readonly CleanupPlan _plan;
    private readonly SceneCatalog _catalog;
    private readonly LifetimeScope _rootScope;

    public LoadSceneStepWithResolver(
        IProgress<float> progress,
        CleanupPlan plan,
        SceneCatalog catalog,
        LifetimeScope rootScope)
    {
        _progress = progress;
        _plan = plan;
        _catalog = catalog;
        _rootScope = rootScope;
    }

    public async UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        ctx.Request.Events?.Emit(TransitionStatus.LoadDestinationScene);

        if (_catalog == null || !_catalog.TryResolve(ctx.Request.DestinationKey, out var resolved))
            throw new TransitionException(TransitionFailCode.ContentNotFound, $"Unknown destination key: {ctx.Request.DestinationKey}");

        ctx.DestinationHandle = await ctx.Loader.LoadSceneAsync(
            resolved,
            ctx.Request.LoadAdditive,
            ctx.Request.ActivateOnLoad,
            _progress,
            _rootScope,
            ct);

        _plan.DestinationHandle = ctx.DestinationHandle;
        _plan.DestinationLoaded = true;

        ctx.Request.Events?.Emit(TransitionStatus.LoadProgressComplete);
    }
}