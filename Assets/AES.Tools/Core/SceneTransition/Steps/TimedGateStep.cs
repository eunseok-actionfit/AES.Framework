using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class TimedGateStep : ITransitionStep
{
    private readonly GateId _id;
    private readonly int _timeoutMs;

    public TimedGateStep(GateId id, int timeoutMs)
    {
        _id = id;
        _timeoutMs = timeoutMs;
    }

    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
        => ctx.Gates.WaitWithTimeout(_id, _timeoutMs, ct);
}