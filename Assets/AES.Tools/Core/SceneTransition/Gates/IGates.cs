using System.Threading;
using Cysharp.Threading.Tasks;

public interface IGates
{
    void Hold(GateId id);
    void Release(GateId id);
    UniTask Wait(GateId id, CancellationToken ct);
}