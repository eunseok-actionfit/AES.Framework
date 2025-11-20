#if AESFW_UNITASK
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools
{
    public static class UniTaskExtensions
    {
        // 안전한 fire-and-forget (로그 포함)
        public static void ForgetWithLog(this UniTask task, string tag = null, bool ignoreCancellation = true)
        {
            task.Forget(ex => {
                if (ignoreCancellation && ex is OperationCanceledException) return;
                Debug.LogError($"[UniTask] {tag ?? "Task"} error: {ex}");
            });
        }


        public async static UniTask<T> WithTimeout<T>(this UniTask<T> task, TimeSpan timeout, CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var delay = UniTask.Delay(timeout, cancellationToken: cts.Token);
            var completed = await UniTask.WhenAny(task, delay);

            if (completed.hasResultLeft)
            {
                cts.Cancel();
                return completed.result;
            }

            throw new TimeoutException($"Task timed out after {timeout.TotalMilliseconds}ms");
        }

        public async static UniTask WithTimeout(this UniTask task, TimeSpan timeout, CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var delay = UniTask.Delay(timeout, cancellationToken: cts.Token);
            var completed = await UniTask.WhenAny(task, delay);

            if (completed == 0)
            {
                cts.Cancel();
                return;
            }

            throw new TimeoutException($"Task timed out after {timeout.TotalMilliseconds}ms");
        }
        
        public static UniTask<Result> AsUniTask(this Result result)
        {
            return UniTask.FromResult(result);
        }
        
        public async static UniTask<Result<T>> BindAsync<T>(
            this UniTask<Result> task,
            Func<UniTask<Result<T>>> next)
        {
            var r = await task;
            return r.IsSuccess ? await next() : Result<T>.Fail(r.Error);
        }

        public async static UniTask<Result<U>> MapAsync<T,U>(
            this UniTask<Result<T>> task,
            Func<T, U> mapper)
        {
            var r = await task;
            return r.IsSuccess ? Result<U>.Ok(mapper(r.Value)) : Result<U>.Fail(r.Error);
        }

    }
}

#endif