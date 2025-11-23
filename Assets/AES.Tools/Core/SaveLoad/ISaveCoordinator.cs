using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools.Core
{

    public interface ISaveCoordinator
    {
        UniTask<Result> SaveAllAsync(CancellationToken ct = default);
        UniTask<Result> LoadAllAsync(CancellationToken ct = default);
    }
}

