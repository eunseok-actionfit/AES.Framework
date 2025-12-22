using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class TransitionPipeline
{
    private readonly List<ITransitionStep> _steps = new();

    public TransitionPipeline Add(ITransitionStep step)
    {
        if (step != null) _steps.Add(step);
        return this;
    }

    public async UniTask Run(TransitionContext ctx, CancellationToken ct)
    {
        foreach (var s in _steps)
        {
            ct.ThrowIfCancellationRequested();
            await s.Execute(ctx, ct);
        }
    }
}