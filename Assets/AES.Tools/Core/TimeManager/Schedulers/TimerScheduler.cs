using System;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools.TimeManager.Schedulers
{
    /// <summary>
    /// ITimerScheduler 구현체
    /// - UniTask 기반
    /// - 안전한 예외 처리 포함
    /// </summary>
    public sealed class TimerScheduler : ITimerScheduler
    {
        public IDisposable RunEvery(
            TimeSpan interval,
            Func<CancellationToken, UniTask> tick,
            bool runImmediately = false,
            CancellationToken external = default)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(external);
            Loop(cts.Token).Forget();
            return new Stopper(cts);

            async UniTaskVoid Loop(CancellationToken ct)
            {
                if (runImmediately)
                    await SafeTick(ct);

                while (!ct.IsCancellationRequested)
                {
                    await UniTask.Delay(interval, cancellationToken: ct);
                    if (ct.IsCancellationRequested) break;
                    await SafeTick(ct);
                }
            }

            async UniTask SafeTick(CancellationToken ct)
            {
                try { await tick(ct); }
                catch (OperationCanceledException) { /* 무시 */ }
                catch (Exception e) { UnityEngine.Debug.LogException(e); }
            }
        }

        public async UniTask RunAfter(
            TimeSpan delay,
            Func<CancellationToken, UniTask> action,
            CancellationToken external = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(external);
            await UniTask.Delay(delay, cancellationToken: cts.Token);

            if (!cts.IsCancellationRequested)
                await action(cts.Token);
        }

        /// <summary>
        /// IDisposable Stopper
        /// - Dispose 시 타이머 중단
        /// </summary>
        private sealed class Stopper : IDisposable
        {
            private CancellationTokenSource _cts;
            public Stopper(CancellationTokenSource cts) => _cts = cts;

            public void Dispose()
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;
            }
        }
    }
}