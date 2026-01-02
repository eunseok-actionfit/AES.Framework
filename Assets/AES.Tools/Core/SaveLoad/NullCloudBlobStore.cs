using System.Threading;
using AES.Tools.TBC.Result;
using Cysharp.Threading.Tasks;


// TODO GPGS, FirebaseStorge 등 클라우드 서비스 구현
namespace AES.Tools
{
    public class NullCloudBlobStore : ICloudBlobStore
    {
        public UniTask<byte[]> LoadOrNullAsync(string key, CancellationToken ct = default) => UniTask.FromResult<byte[]>(null);

        public UniTask SaveAsync(string key, byte[] bytes, CancellationToken ct = default) => UniTask.CompletedTask;
        public UniTask DeleteAsync(string key, CancellationToken ct = default) =>  UniTask.CompletedTask;
    }
}


