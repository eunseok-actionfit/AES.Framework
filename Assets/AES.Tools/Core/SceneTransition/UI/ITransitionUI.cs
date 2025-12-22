public interface ITransitionUI
{
    void SetStatus(string messageKey);
    void SetRetryVisible(bool visible);
    void SetClearCacheVisible(bool visible);

    void BindRetry(System.Action onRetry);
    void BindClearCache(System.Action onClearCache);
}