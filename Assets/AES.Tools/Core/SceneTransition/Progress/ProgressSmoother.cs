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

    public ProgressSmoother(Func<float, float> speedFn, IProgress<float> realtimeSink, IProgress<float> smoothedSink)
    {
        _speedFn = speedFn ?? (_ => 5f);
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
        var speed = Mathf.Max(0.01f, _speedFn(Smoothed));
        Smoothed = Mathf.MoveTowards(Smoothed, Realtime, unscaledDeltaTime * speed);
        _smoothedSink?.Report(Smoothed);
    }

    public async UniTask WaitUntilFilled(CancellationToken ct)
    {
        while (Smoothed < 0.999f)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield();
        }
    }

    public void Dispose() { }
}