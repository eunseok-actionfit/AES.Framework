using System.Threading;
using Cysharp.Threading.Tasks;


namespace Core.Systems.Storage.Core
{
    public interface ISaveCoordinator
    {
        UniTask LoadAllAsync(CancellationToken ct = default);
        UniTask SaveAllAsync(CancellationToken ct = default);
    }
}

