namespace AES.Tools.VContainer.Bootstrap.Framework
{
    public interface IFeatureProgressReporter
    {
        // localNormalized: 0..1 (feature 내부 진행률)
        void Report(float localNormalized, string message = null);

        // 단계만 찍고 싶을 때(예: step count 기반)
        void Step(string message = null);
    }
}