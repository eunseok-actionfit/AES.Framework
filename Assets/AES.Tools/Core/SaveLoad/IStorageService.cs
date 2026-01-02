using System.Threading;
using AES.Tools.TBC.Result;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public interface IStorageService
    {
        UniTask<T> LoadAsync<T>(string slotId = null, CancellationToken ct = default);
        UniTask SaveAsync<T>(T data, CancellationToken ct = default);
        UniTask SaveAsync<T>(string slotId, T data, CancellationToken ct = default);
         UniTask DeleteAsync<T>(string slotId = null, CancellationToken ct = default);
    }
}


