using System.Threading;
using AES.Tools.TBC.Result;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public interface IBlobStore
    {
        UniTask<byte[]> LoadOrNullAsync(string key, CancellationToken ct = default);
        UniTask SaveAsync(string key, byte[] bytes, CancellationToken ct = default);
        UniTask DeleteAsync(string key, CancellationToken ct = default);
    }
    
    public interface ILocalBlobStore : IBlobStore { }
    public interface ICloudBlobStore : IBlobStore { }
}

