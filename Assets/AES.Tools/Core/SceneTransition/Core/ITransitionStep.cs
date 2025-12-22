using System.Threading;
using Cysharp.Threading.Tasks;

public interface ITransitionStep
{
    UniTask Execute(TransitionContext ctx, CancellationToken ct);
}