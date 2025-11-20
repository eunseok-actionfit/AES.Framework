using System.IO;
using AES.Tools.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Impl
{
    public class FileBlobStore : ILocalBlobStore
    {
        readonly string rootPath;

        public FileBlobStore()
        {
            rootPath = Application.persistentDataPath;
        }

        string GetPath(string key) => Path.Combine(rootPath, key + ".bin");

        public async UniTask<byte[]> LoadOrNullAsync(string key)
        {
            var path = GetPath(key);
            if (!File.Exists(path)) return null;
            
            return await UniTask.RunOnThreadPool(() => File.ReadAllBytes(path));
        }

        public async UniTask SaveAsync(string key, byte[] data)
        {
            var path = GetPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await UniTask.RunOnThreadPool(() => File.WriteAllBytes(path, data));
        }

        public async UniTask DeleteAsync(string key)
        {
           var path = GetPath(key);
           if(!File.Exists(path)) return;
           await UniTask.RunOnThreadPool(() => File.Delete(path));
        }
    }
}


