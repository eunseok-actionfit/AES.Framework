using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AES.Tools
{
    public class FileBlobStore : ILocalBlobStore
    {
        private readonly string rootPath = Application.persistentDataPath;

        // key별 동시성 제어(프로세스 내)
        private static readonly Dictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

        private static SemaphoreSlim GetLock(string key)
        {
            lock (_locks)
            {
                if (!_locks.TryGetValue(key, out var sem))
                {
                    sem = new SemaphoreSlim(1, 1);
                    _locks[key] = sem;
                }
                return sem;
            }
        }

        string GetPath(string key) => Path.Combine(rootPath, key + ".bin");

        public async UniTask<byte[]> LoadOrNullAsync(string key, CancellationToken ct = default)
        {
            var sem = GetLock(key);
            await sem.WaitAsync(ct);
            try
            {
                var path = GetPath(key);
                if (!File.Exists(path)) return null;

                return await UniTask.RunOnThreadPool(() =>
                {
                    ct.ThrowIfCancellationRequested();
                    return File.ReadAllBytes(path);
                }, cancellationToken: ct);
            }
            finally
            {
                sem.Release();
            }
        }

        public async UniTask SaveAsync(string key, byte[] bytes, CancellationToken ct = default)
        {
            var sem = GetLock(key);
            await sem.WaitAsync(ct);
            try
            {
                var path = GetPath(key);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var tmpPath = path + ".tmp";

                await UniTask.RunOnThreadPool(() =>
                {
                    ct.ThrowIfCancellationRequested();

                    // 1) tmp에 먼저 쓴다
                    File.WriteAllBytes(tmpPath, bytes);

                    // 2) 원본 교체(가능하면 Replace)
                    // File.Replace는 대상이 없으면 예외이므로 분기 처리
                    if (File.Exists(path))
                    {
                        // backup은 null로 (원하면 ".bak"로 남길 수 있음)
                        File.Replace(tmpPath, path, null);
                    }
                    else
                    {
                        File.Move(tmpPath, path);
                    }
                }, cancellationToken: ct);
            }
            finally
            {
                sem.Release();
            }
        }

        public async UniTask DeleteAsync(string key, CancellationToken ct = default)
        {
            var sem = GetLock(key);
            await sem.WaitAsync(ct);
            try
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
            finally
            {
                sem.Release();
            }
        }
    }
}
