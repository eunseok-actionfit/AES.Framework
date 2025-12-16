using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    public abstract class AppFeatureSO : ScriptableObject, IAppFeature
    {
        [SerializeField] private string id;
        [SerializeField] private int order;
        [SerializeField] private string[] dependsOn;
        [SerializeField] private bool enabledByDefault = true;

        public string Id => id;
        public int Order => order;
        public string[] DependsOn => dependsOn;
        public bool EnabledByDefault => enabledByDefault;

        public virtual bool IsEnabled(in FeatureContext ctx) => true;

        public virtual void Install(IContainerBuilder builder, in FeatureContext ctx) { }
        public virtual UniTask Initialize(LifetimeScope rootScope,  FeatureContext ctx) => UniTask.CompletedTask;
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
                id = name;

            id = id.Trim();
        }
#endif
    }
}