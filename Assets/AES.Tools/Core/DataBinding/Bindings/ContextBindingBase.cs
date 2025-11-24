using UnityEngine;

namespace AES.Tools
{
    public enum MemberPathMode
    {
        Custom,
        Dropdown
    }

    /// <summary>
    /// DataContextBase를 찾아 IBindingContext + memberPath를 resolve해 주는 바인딩 베이스.
    /// 실제 바인딩 로직은 파생 클래스에서 IBindingContext.RegisterListener(path) 기반으로 구현한다.
    /// </summary>
    public abstract class ContextBindingBase : BindingBehaviour
    {
        [SerializeField] ContextLookupMode lookupMode = ContextLookupMode.Nearest;
        [SerializeField] string contextName;

        [SerializeField] MemberPathMode memberPathMode = MemberPathMode.Dropdown;
        [SerializeField] string memberPath;

        protected DataContextBase CurrentContext { get; private set; }
        protected IBindingContext BindingContext => CurrentContext?.BindingContext;

        protected string ResolvedPath => memberPath;

        protected override void Subscribe()
        {
#if UNITY_EDITOR
            Debug_ClearRuntimeInfo();
#endif

            CurrentContext = ResolveContext();

#if UNITY_EDITOR
            Debug_SetContextAndPath(CurrentContext, memberPath);
#endif

            if (CurrentContext == null || BindingContext == null)
            {
                LogBindingError("DataContext 또는 BindingContext를 찾지 못했습니다.");
                return;
            }

            OnContextAvailable(BindingContext, memberPath);
        }

        protected override void Unsubscribe()
        {
            OnContextUnavailable();
        }

        protected abstract void OnContextAvailable(IBindingContext context, string path);
        protected abstract void OnContextUnavailable();

        DataContextBase ResolveContext()
        {
            switch (lookupMode)
            {
                case ContextLookupMode.Nearest:
                    return GetComponentInParent<DataContextBase>();

                case ContextLookupMode.ByNameInParents:
                {
                    if (string.IsNullOrEmpty(contextName))
                        return null;

                    var parents = GetComponentsInParent<DataContextBase>(includeInactive: true);
                    foreach (var ctx in parents)
                        if (ctx.ContextName == contextName)
                            return ctx;

                    return null;
                }

                case ContextLookupMode.ByNameInScene:
                {
                    if (string.IsNullOrEmpty(contextName))
                        return null;

#if UNITY_2022_2_OR_NEWER
                    var all = FindObjectsByType<DataContextBase>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                    var all = FindObjectsOfType<DataContextBase>(true);
#endif
                    foreach (var ctx in all)
                        if (ctx.ContextName == contextName)
                            return ctx;

                    return null;
                }

                default:
                    return GetComponentInParent<DataContextBase>();
            }
        }
    }
}
