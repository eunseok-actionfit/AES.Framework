using System.Threading;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

public sealed class LoadingScenePresenter : ILoadingPresenter
{
    private readonly LifetimeScope _rootScope;

    public LoadingScenePresenter(LifetimeScope rootScope)
    {
        _rootScope = rootScope;
    }

    public async UniTask ShowAsync(TransitionContext ctx, CleanupPlan plan, CancellationToken ct)
    {
        if (!ctx.Request.ShowLoadingScreen)
            return;

        if (plan.LoadingLoaded)
            return;

        var key = ctx.LoadingKey;

        if (string.IsNullOrEmpty(key.UnityKey) && string.IsNullOrEmpty(key.AddressablesKey))
            return;

        var resolved = new ResolvedSceneKey(
            forUnity: key.UnityKey,
            forAddressables: key.AddressablesKey,
            isAddressable: key.IsAddressable);

        var handle = await ctx.Loader.LoadSceneAsync(
            resolved,
            additive: true,
            activateOnLoad: true,
            progress: null,
            parentScope: _rootScope,
            ct);

        plan.LoadingHandle = handle;
        plan.LoadingLoaded = true;
    }

    public async UniTask HideAsync(TransitionContext ctx, CleanupPlan plan, CancellationToken ct)
    {
        if (!plan.LoadingLoaded)
            return;

        try
        {
            await ctx.Loader.UnloadAsync(plan.LoadingHandle, ct);
        }
        catch { }

        plan.LoadingLoaded = false;
    }
}