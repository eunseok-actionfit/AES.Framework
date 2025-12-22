public static class TransitionFailureClassifier
{
    public static TransitionFailCode Classify(System.Exception e)
    {
        if (e is System.OperationCanceledException) return TransitionFailCode.Canceled;
        if (e is TransitionException te) return te.Code;

        var m = (e?.Message ?? "").ToLowerInvariant();
        if (m.Contains("gate timeout")) return TransitionFailCode.ServerTimeout;
        if (m.Contains("auth") || m.Contains("unauthorized") || m.Contains("forbidden")) return TransitionFailCode.ServerRejected;

        return TransitionFailCode.Unknown;
    }
}