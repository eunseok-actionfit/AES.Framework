using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools.Core
{
    public interface IStorageService
    {
        UniTask<Result<T>> LoadAsync<T>(string slotId, CancellationToken ct = default);
        UniTask<Result> SaveAsync<T>(string slotId, T data, CancellationToken ct = default);
    }
}


