using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ILoadingProgressHub
{
    IDisposable PushRange(float min01, float max01);
    void ReportRealtime01(float p01);
    void SetMessage(string msg);

    UniTask WaitUntilFilledAsync(CancellationToken ct);

    void Stop();
}

public sealed class LoadingProgressHub : ILoadingProgressHub, IDisposable
{
    private readonly object _lock = new();

    private struct RangeState
    {
        public float Min;
        public float Max;
        public RangeState(float min, float max) { Min = min; Max = max; }
    }

    private RangeState[] _stack = new RangeState[8];
    private int _stackCount;

    private float _rangeMin = 0f;
    private float _rangeMax = 1f;

    private float _lastRealtime;
    private float _lastSmoothed;

    private readonly ProgressSmoother _smoother;
    private CancellationTokenSource _tickCts;

    public LoadingProgressHub()
    {
        var realtimeSink = new System.Progress<float>(p => _lastRealtime = Mathf.Clamp01(p));
        var smoothedSink = new System.Progress<float>(p =>
        {
            _lastSmoothed = Mathf.Clamp01(p);
            LoadingUIRegistry.Current?.SetProgress(_lastRealtime, _lastSmoothed);
        });

        // 여기 speed는 "반응성 k"로 해석됨 (지수 스무딩)
        _smoother = new ProgressSmoother(_ => 8f, realtimeSink, smoothedSink);
    }

    public IDisposable PushRange(float min01, float max01)
    {
        min01 = Mathf.Clamp01(min01);
        max01 = Mathf.Clamp01(max01);
        if (max01 < min01) (min01, max01) = (max01, min01);

        lock (_lock)
        {
            EnsureTick();

            PushState(new RangeState(_rangeMin, _rangeMax));
            _rangeMin = min01;
            _rangeMax = max01;
        }

        return new PopRange(this);
    }

    public void ReportRealtime01(float p01)
    {
        lock (_lock)
        {
            EnsureTick();
            var x = Mathf.Clamp01(p01);
            var mapped = Mathf.Lerp(_rangeMin, _rangeMax, x);
            _smoother.SetRealtime(mapped);
        }
    }

    public void SetMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        LoadingUIRegistry.Current?.SetMessage(msg);
    }

    public async UniTask WaitUntilFilledAsync(CancellationToken ct)
    {
        lock (_lock) EnsureTick();
        
        const float minSeconds = 0.35f;

        // Hub 내부 tick는 unscaledDeltaTime을 쓰므로 unscaled로 기다림
        await UniTask.Delay(TimeSpan.FromSeconds(minSeconds), DelayType.UnscaledDeltaTime,  cancellationToken:ct);

        // 그 다음 실제로 smoothed가 채워질 때까지 대기
        await _smoother.WaitUntilFilled(ct);
    }

    public void Stop()
    {
        lock (_lock)
        {
            _stackCount = 0;
            _rangeMin = 0f;
            _rangeMax = 1f;

            _tickCts?.Cancel();
            _tickCts?.Dispose();
            _tickCts = null;
        }
    }

    private void EnsureTick()
    {
        if (_tickCts != null) return;

        _tickCts = new CancellationTokenSource();
        var ct = _tickCts.Token;

        UniTask.Create(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                _smoother.Tick(Time.unscaledDeltaTime);
                await UniTask.Yield();
            }
        }).Forget();
    }

    private void PushState(RangeState s)
    {
        if (_stackCount >= _stack.Length)
            Array.Resize(ref _stack, _stack.Length * 2);

        _stack[_stackCount++] = s;
    }

    private RangeState PopState()
    {
        if (_stackCount <= 0) return new RangeState(0f, 1f);
        return _stack[--_stackCount];
    }

    public void Dispose()
    {
        Stop();
        _smoother.Dispose();
    }

    private sealed class PopRange : IDisposable
    {
        private LoadingProgressHub _hub;
        public PopRange(LoadingProgressHub hub) => _hub = hub;

        public void Dispose()
        {
            var hub = _hub;
            if (hub == null) return;
            _hub = null;

            lock (hub._lock)
            {
                var prev = hub.PopState();
                hub._rangeMin = prev.Min;
                hub._rangeMax = prev.Max;
            }
        }
    }
}
