public static class TransitionUIBinder
{
    public static void ApplyStatus(ITransitionUI ui, TransitionStatus status)
    {
        if (ui == null) return;

        // 프로젝트 로컬라이징 키로 그대로 사용 가능
        ui.SetStatus(status.ToString());
    }

    public static void ApplyFailurePolicy(ITransitionUI ui, FailurePolicy policy)
    {
        if (ui == null || policy == null) return;

        ui.SetStatus(policy.UiMessageKey);
        ui.SetRetryVisible(policy.SuggestRetry);
        ui.SetClearCacheVisible(policy.ClearCacheSuggestion);
    }
}