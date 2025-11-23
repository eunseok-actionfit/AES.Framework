using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools.Core
{
    public interface IBlobStore
    {
        UniTask<byte[]> LoadOrNullAsync(string key, CancellationToken ct = default);
        UniTask<Result> SaveAsync(string key, byte[] bytes, CancellationToken ct = default);
        UniTask<Result> DeleteAsync(string key, CancellationToken ct = default);
    }
    
    public interface ILocalBlobStore : IBlobStore { }
    public interface ICloudBlobStore : IBlobStore { }
}

