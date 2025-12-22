using System.Threading;
using Cysharp.Threading.Tasks;

public interface ILoadingPresenter
{
    UniTask ShowAsync(TransitionContext ctx, CleanupPlan plan, CancellationToken ct);
    UniTask HideAsync(TransitionContext ctx, CleanupPlan plan, CancellationToken ct);
}