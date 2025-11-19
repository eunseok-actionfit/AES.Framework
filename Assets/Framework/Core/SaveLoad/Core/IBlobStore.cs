using Cysharp.Threading.Tasks;


namespace Core.Systems.Storage.Core
{
    public interface IBlobStore
    {
        UniTask<byte[]> LoadOrNullAsync(string key);
        UniTask SaveAsync(string key, byte[] data);
        UniTask DeleteAsync(string key);
    }
    
    public interface ILocalBlobStore : IBlobStore { }
    public interface ICloudBlobStore : IBlobStore { }
}

