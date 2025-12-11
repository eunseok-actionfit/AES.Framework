using System;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    /// <summary>
    /// 타이머 스케줄러 인터페이스
    /// - 일정 간격 반복 실행
    /// - 일정 지연 후 단일 실행
    /// </summary>
    public interface ITimerScheduler
    {
        /// <summary>
        /// interval마다 반복 호출.
        /// 반환값 IDisposable.Dispose()로 중단 가능.
        /// </summary>
        IDisposable RunEvery(
            TimeSpan interval,
            Func<CancellationToken, UniTask> tick,
            bool runImmediately = false,
            CancellationToken external = default);

        /// <summary>
        /// 지정된 delay 이후 단발 실행.
        /// </summary>
        UniTask RunAfter(
            TimeSpan delay,
            Func<CancellationToken, UniTask> action,
            CancellationToken external = default);
    }

}