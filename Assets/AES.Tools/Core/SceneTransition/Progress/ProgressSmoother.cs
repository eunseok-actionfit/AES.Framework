using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class ProgressSmoother : IDisposable
{
    public float Realtime { get; private set; }
    public float Smoothed { get; private set; }

    private readonly Func<float, float> _speedFn;
    private readonly IProgress<float> _realtimeSink;
    private readonly IProgress<float> _smoothedSink;

    // "완료"로 간주할 임계치
    private const float FilledThreshold = 0.999f;

    public ProgressSmoother(Func<float, float> speedFn, IProgress<float> realtimeSink, IProgress<float> smoothedSink)
    {
        _speedFn = speedFn ?? (_ => 8f);   // 기존 5f보다 약간 높은 기본값 권장(지수 스무딩은 느낌이 다름)
        _realtimeSink = realtimeSink;
        _smoothedSink = smoothedSink;
    }

    public void SetRealtime(float v)
    {
        Realtime = Mathf.Clamp01(v);
        _realtimeSink?.Report(Realtime);
    }

    public void Tick(float unscaledDeltaTime)
    {
        // 지수 스무딩: dt에 독립적인 부드러운 수렴
        // k가 클수록 Realtime을 더 빨리 따라감
        var k = Mathf.Max(0.01f, _speedFn(Smoothed));
        var t = 1f - Mathf.Exp(-k * Mathf.Max(0f, unscaledDeltaTime));

        Smoothed = Mathf.Lerp(Smoothed, Realtime, t);

        // 끝에서 "무한 수렴"으로 인해 절대 1.0이 안 되는 케이스 방지용 스냅
        if (Realtime >= FilledThreshold && Smoothed >= FilledThreshold)
            Smoothed = 1f;

        _smoothedSink?.Report(Smoothed);
    }

    public async UniTask WaitUntilFilled(CancellationToken ct)
    {
        while (Smoothed < FilledThreshold)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }
    }

    public void Dispose() { }
}