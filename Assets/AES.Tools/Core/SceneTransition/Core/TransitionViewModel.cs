using System;
using AES.Tools;


public sealed class TransitionViewModel
{
    // ---- Progress ----
    // Realtime01: 로더가 보고한 실제 진행률
    // Smoothed01: UX용 스무딩 진행률(ProgressSmoother 결과)
    public ObservableProperty<float> Realtime01 { get; } = new();
    public ObservableProperty<float> Smoothed01 { get; } = new();

    // ---- Text/Status ----
    public ObservableProperty<string> Message { get; } = new("");
    public ObservableProperty<TransitionStatus> Status { get; } = new();

    // ---- Buttons ----
    public ObservableProperty<bool> RetryVisible { get; } = new();
    public ObservableProperty<bool> ClearCacheVisible { get; } = new();

    
    public readonly Command RetryCommand;
    public readonly Command ClearCacheCommand;
    
    // ---- Requests (View → VM → Service) ----
    // Command를 VM이 직접 생성하면, execute를 Service가 주입해야 해서 결합이 생김.
    // 그래서 VM은 요청 이벤트만 제공하고, Service가 구독해 처리한다.
    public event Action RetryRequested = delegate { };
    public event Action ClearCacheRequested = delegate { };
    
    public TransitionViewModel()
    {
        RetryCommand = new Command(RequestRetry);
        ClearCacheCommand = new Command(RequestClearCache);
    }

    public void ResetForNewRun()
    {
        Realtime01.Value = 0f;
        Smoothed01.Value = 0f;
        Message.Value = "";
        Status.Value = TransitionStatus.LoadStarted;

        RetryVisible.Value = false;
        ClearCacheVisible.Value = false;
    }

    public void SetProgress(float realtime01, float smoothed01)
    {
        Realtime01.Value = Clamp01(realtime01);
        Smoothed01.Value = Clamp01(smoothed01);
    }

    public void SetMessage(string msg) => Message.Value = msg ?? "";
    public void SetStatus(TransitionStatus status) => Status.Value = status;

    public void SetRetryVisible(bool visible) => RetryVisible.Value = visible;
    public void SetClearCacheVisible(bool visible) => ClearCacheVisible.Value = visible;
    
    public void RequestRetry() => RetryRequested();
    public void RequestClearCache() => ClearCacheRequested();

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
}