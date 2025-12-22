using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class GateStep : ITransitionStep
{
    private readonly GateId _id;
    public GateStep(GateId id) { _id = id; }

    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
        => ctx.Gates.Wait(_id, ct);
}