using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools.Core
{
    public interface ISaveCoordinator
    {
        UniTask LoadAllAsync(CancellationToken ct = default);
        UniTask SaveAllAsync(CancellationToken ct = default);
    }
}

