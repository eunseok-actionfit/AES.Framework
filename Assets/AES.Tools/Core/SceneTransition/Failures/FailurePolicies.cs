public static class FailurePolicies
{
    public static FailurePolicy Get(TransitionFailCode code) => code switch
    {
        TransitionFailCode.Canceled => new FailurePolicy
        {
            DoFallback = false, UiMessageKey = "CANCELED", SuggestRetry = false, ClearCacheSuggestion = false
        },

        TransitionFailCode.ServerTimeout => new FailurePolicy
        {
            DoFallback = true, UiMessageKey = "SERVER_TIMEOUT", SuggestRetry = true, ClearCacheSuggestion = false
        },

        TransitionFailCode.ServerRejected => new FailurePolicy
        {
            DoFallback = true, UiMessageKey = "AUTH_REQUIRED", SuggestRetry = false, ClearCacheSuggestion = false
        },

        TransitionFailCode.ContentNotFound => new FailurePolicy
        {
            DoFallback = true, UiMessageKey = "CONTENT_MISSING", SuggestRetry = false, ClearCacheSuggestion = true
        },

        TransitionFailCode.ContentDownloadFailed => new FailurePolicy
        {
            DoFallback = true, UiMessageKey = "DOWNLOAD_FAILED", SuggestRetry = true, ClearCacheSuggestion = true
        },

        TransitionFailCode.SceneLoadFailed => new FailurePolicy
        {
            DoFallback = true, UiMessageKey = "SCENE_LOAD_FAILED", SuggestRetry = true, ClearCacheSuggestion = true
        },

        TransitionFailCode.InitializationCrashed => new FailurePolicy
        {
            DoFallback = true, UiMessageKey = "INIT_CRASHED", SuggestRetry = false, ClearCacheSuggestion = false
        },

        _ => new FailurePolicy
        {
            DoFallback = true, UiMessageKey = "UNKNOWN_ERROR", SuggestRetry = true, ClearCacheSuggestion = false
        },
    };
}