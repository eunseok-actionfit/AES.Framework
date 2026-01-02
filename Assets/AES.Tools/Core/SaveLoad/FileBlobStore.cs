using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools
{
    public class FileBlobStore : ILocalBlobStore
    {
        private readonly string rootPath = Application.persistentDataPath;

        string GetPath(string key) => Path.Combine(rootPath, key + ".bin");

        // IBlobStore 구현: CancellationToken 포함
        public async UniTask<byte[]> LoadOrNullAsync(string key, CancellationToken ct = default)
        {
            var path = GetPath(key);
            if (!File.Exists(path)) return null;

            return await UniTask.RunOnThreadPool(() =>
            {
                ct.ThrowIfCancellationRequested();
                return File.ReadAllBytes(path);
            }, cancellationToken: ct);
        }

        // Result 제거: 실패 시 예외 throw
        public async UniTask SaveAsync(string key, byte[] bytes, CancellationToken ct = default)
        {
            var path = GetPath(key);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await UniTask.RunOnThreadPool(() =>
            {
                ct.ThrowIfCancellationRequested();
                File.WriteAllBytes(path, bytes);
            }, cancellationToken: ct);
        }

        // Result 제거: 실패 시 예외 throw
        public async UniTask DeleteAsync(string key, CancellationToken ct = default)
        {
            var path = GetPath(key);

            if (!File.Exists(path))
                return;

            await UniTask.RunOnThreadPool(() =>
            {
                ct.ThrowIfCancellationRequested();
                File.Delete(path);
            }, cancellationToken: ct);
        }
    }
}