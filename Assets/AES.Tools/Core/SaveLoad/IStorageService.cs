using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools.Core
{
    public interface IStorageService
    {
        UniTask<Result<T>> LoadAsync<T>(string slotId = null, CancellationToken ct = default);
        UniTask<Result> SaveAsync<T>(T data, CancellationToken ct = default);
        UniTask<Result> SaveAsync<T>(string slotId, T data, CancellationToken ct = default);
         UniTask<Result> DeleteAsync<T>(string slotId = null, CancellationToken ct = default);
    }
}


