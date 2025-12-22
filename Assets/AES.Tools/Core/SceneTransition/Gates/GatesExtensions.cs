using System.Threading;
using Cysharp.Threading.Tasks;

public static class GatesExtensions
{
    public async static UniTask WaitWithTimeout(this IGates gates, GateId id, int timeoutMs, CancellationToken ct)
    {
        if (timeoutMs <= 0)
        {
            await gates.Wait(id, ct);
            return;
        }

        var waitTask = gates.Wait(id, ct);
        var timeoutTask = UniTask.Delay(timeoutMs, ignoreTimeScale: true, cancellationToken: ct);

        var winner = await UniTask.WhenAny(waitTask, timeoutTask);
        if (winner == 1)
            throw new TransitionException(TransitionFailCode.ServerTimeout, $"Gate timeout: {id} ({timeoutMs}ms)");
    }
}