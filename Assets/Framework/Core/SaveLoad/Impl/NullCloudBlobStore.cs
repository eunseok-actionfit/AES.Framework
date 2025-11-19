using Core.Systems.Storage.Core;
using Cysharp.Threading.Tasks;


// TODO GPGS, FirebaseStorge 등 클라우드 서비스 구현
namespace Core.Systems.Storage.Impl
{
    public class NullCloudBlobStore : ICloudBlobStore
    {
        public UniTask<byte[]> LoadOrNullAsync(string key) => UniTask.FromResult<byte[]>(null);

        public UniTask SaveAsync(string key, byte[] data) => UniTask.CompletedTask;

        public UniTask DeleteAsync(string key) => UniTask.CompletedTask;
    }
}


