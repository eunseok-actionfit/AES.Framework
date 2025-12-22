using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public sealed class CleanupStep : ITransitionStep
{
    private readonly CleanupPlan _plan;
    private readonly bool _force;

    public CleanupStep(CleanupPlan plan, bool force)
    {
        _plan = plan;
        _force = force;
    }

    public async UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        if (!_force) return;

        if (_plan.DestinationLoaded)
        {
            try { await ctx.Loader.UnloadAsync(_plan.DestinationHandle, ct); }
            catch
            { // ignored
            }
        }

        // loading handle cleanup
        if (_plan.LoadingLoaded)
        {
            try { await ctx.Loader.UnloadAsync(_plan.LoadingHandle, ct); }
            catch
            { // ignored
            }
            finally { _plan.LoadingLoaded = false; }
        }

        if (_plan.RestorePreviousActiveScene && _plan.PreviousActiveScene.IsValid() && _plan.PreviousActiveScene.isLoaded)
        {
            try { SceneManager.SetActiveScene(_plan.PreviousActiveScene); }
            catch
            { // ignored
            }
        }
    }
}