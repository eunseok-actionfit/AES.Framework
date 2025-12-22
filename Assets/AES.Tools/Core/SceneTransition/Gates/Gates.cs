using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class Gates : IGates
{
    private readonly Dictionary<GateId, UniTaskCompletionSource> _map = new();

    public void Hold(GateId id)
    {
        if (!_map.ContainsKey(id)) _map[id] = new UniTaskCompletionSource();
    }

    public void Release(GateId id)
    {
        if (_map.TryGetValue(id, out var tcs))
        {
            tcs.TrySetResult();
            _map.Remove(id);
        }
    }

    public UniTask Wait(GateId id, CancellationToken ct)
    {
        return _map.TryGetValue(id, out var tcs)
            ? tcs.Task.AttachExternalCancellation(ct)
            : UniTask.CompletedTask;
    }
}