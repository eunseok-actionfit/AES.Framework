using System;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    internal sealed class FeatureProgressReporter : IFeatureProgressReporter
    {
        private readonly IProgress<BootstrapProgress> _outer;
        private readonly int _index;
        private readonly int _total;
        private readonly string _featureId;
        private readonly bool _enabled;

        private int _step;
        private int _stepHintTotal;

        public FeatureProgressReporter(
            IProgress<BootstrapProgress> outer,
            int index,
            int total,
            string featureId,
            bool enabled,
            int stepHintTotal = 0)
        {
            _outer = outer;
            _index = index;
            _total = total <= 0 ? 1 : total;
            _featureId = featureId;
            _enabled = enabled;
            _stepHintTotal = stepHintTotal;
            _step = 0;
        }

        public void Report(float localNormalized, string message = null)
        {
            if (_outer == null) return;

            var local = Clamp01(localNormalized);
            var normalized = (_index + local) / _total;

            _outer.Report(new BootstrapProgress(
                normalized: normalized,
                local: local,
                index: _index,
                total: _total,
                featureId: _featureId,
                enabled: _enabled,
                phase: BootstrapProgressPhase.FeatureProgress,
                message: message,
                exception: null));
        }

        public void Step(string message = null)
        {
            // step hint가 없으면 4단계로 가정(적당히 보임)
            if (_stepHintTotal <= 0) _stepHintTotal = 4;

            _step++;
            var local = Clamp01((float)_step / _stepHintTotal);
            Report(local, message);
        }

        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
