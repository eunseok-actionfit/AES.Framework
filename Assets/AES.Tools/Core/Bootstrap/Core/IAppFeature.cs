using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    public interface IAppFeature
    {
        string Id { get; }              // unique
        int Order { get; }              // fallback order
        string[] DependsOn { get; }     // feature ids
        bool EnabledByDefault { get; }  // profile entry 없을 때 기본값

        bool IsEnabled(in FeatureContext ctx);

        void Install(IContainerBuilder builder, in FeatureContext ctx);
        UniTask Initialize(LifetimeScope rootScope,  FeatureContext ctx);
    }
}