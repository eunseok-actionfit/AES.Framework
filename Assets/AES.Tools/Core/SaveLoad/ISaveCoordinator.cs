using System.Threading;
using AES.Tools.TBC.Result;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{

    public interface ISaveCoordinator
    {
        UniTask<Result> SaveAllAsync(CancellationToken ct = default);
        UniTask<Result> LoadAllAsync(CancellationToken ct = default);
    }
}

