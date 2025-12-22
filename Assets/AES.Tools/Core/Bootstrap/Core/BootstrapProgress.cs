using System;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    public enum BootstrapProgressPhase
    {
        Start,
        FeatureBegin,
        FeatureProgress, // feature 내부 진행률 보고
        FeatureEnd,
        Complete
    }

    public readonly struct BootstrapProgress
    {
        public readonly float Normalized;     // 0..1 (전체)
        public readonly float Local;          // 0..1 (현재 feature 내부)
        public readonly int Index;            // 0-based (feature index)
        public readonly int Total;
        public readonly string FeatureId;     // null 가능
        public readonly bool Enabled;
        public readonly BootstrapProgressPhase Phase;
        public readonly string Message;       // optional
        public readonly Exception Exception;

        public BootstrapProgress(
            float normalized,
            float local,
            int index,
            int total,
            string featureId,
            bool enabled,
            BootstrapProgressPhase phase,
            string message,
            Exception exception)
        {
            Normalized = normalized;
            Local = local;
            Index = index;
            Total = total;
            FeatureId = featureId;
            Enabled = enabled;
            Phase = phase;
            Message = message;
            Exception = exception;
        }
    }
}