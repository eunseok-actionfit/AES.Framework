using System;
using System.IO;
using System.Threading;
using AES.Tools.TBC.Result;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools
{
    public class FileBlobStore : ILocalBlobStore
    {
        private readonly string rootPath = Application.persistentDataPath;

        string GetPath(string key) => System.IO.Path.Combine(rootPath, key + ".bin");

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

        // IBlobStore 구현: UniTask<Result> + CancellationToken
        public async UniTask<Result> SaveAsync(string key, byte[] bytes, CancellationToken ct = default)
        {
            try
            {
                var path = GetPath(key);
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await UniTask.RunOnThreadPool(() =>
                {
                    ct.ThrowIfCancellationRequested();
                    File.WriteAllBytes(path, bytes);
                }, cancellationToken: ct);

                return Result.Ok();
            }
            catch (Exception e)
            {
                // 로컬 저장 실패 에러 래핑
                return Result.Fail(new Error(
                    code: "LOCAL_SAVE_FAILED",
                    message: $"로컬 저장 실패: {key}",
                    context: "local",
                    retriable: false,
                    ex: e
                ));
            }
        }

        public async UniTask<Result> DeleteAsync(string key, CancellationToken ct)
        {
            try
            {
                var path = GetPath(key);

                if (!File.Exists(path))
                    return Result.Ok(); // 이미 없으면 성공 처리

                await UniTask.RunOnThreadPool(() =>
                {
                    ct.ThrowIfCancellationRequested();
                    File.Delete(path);
                }, cancellationToken: ct);

                return Result.Ok();
            }
            catch (Exception e)
            {
                return Result.Fail(new Error(
                    code: "LOCAL_DELETE_FAILED",
                    message: $"로컬 파일 삭제 실패: {key}",
                    context: "local",
                    retriable: false,
                    ex: e
                ));
            }
        }

    }
}
