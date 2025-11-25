using UnityEngine;

namespace AES.Tools
{
    public enum MemberPathMode
    {
        Custom,
        Dropdown
    }
    
    public enum ContextLookupMode
    {
        Nearest,        // 기존처럼 가장 가까운 상위 DataContext
        ByNameInParents,// 부모 계층(DataContextBase들)에서 이름으로 검색
        ByNameInScene,   // 씬 전체에서 이름으로 검색
    }

    /// <summary>
    /// IBindingContextProvider 를 찾아 IBindingContext + memberPath 를 resolve해 주는 바인딩 베이스.
    /// 실제 바인딩 로직은 파생 클래스에서 IBindingContext.RegisterListener(path) 기반으로 구현한다.
    /// </summary>
    public abstract class ContextBindingBase : BindingBehaviour
    {
        [SerializeField] ContextLookupMode lookupMode = ContextLookupMode.Nearest;
        [SerializeField] string contextName;

        [SerializeField] MemberPathMode memberPathMode = MemberPathMode.Dropdown;
        [SerializeField] string memberPath;

        protected IBindingContextProvider CurrentProvider { get; private set; }
        protected IBindingContext BindingContext => CurrentProvider?.RuntimeContext;

        protected string ResolvedPath => memberPath;

        protected override void Subscribe()
        {
#if UNITY_EDITOR
            Debug_ClearRuntimeInfo();
#endif

            CurrentProvider = ResolveProvider();

#if UNITY_EDITOR
            if (CurrentProvider is Object obj)
                Debug_SetContextAndPath(obj, memberPath);
            else
                Debug_SetContextAndPath(null, memberPath);
#endif

            if (BindingContext == null)
            {
                LogBindingError("IBindingContextProvider 또는 BindingContext를 찾지 못했습니다.");
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

        IBindingContextProvider ResolveProvider()
        {
            switch (lookupMode)
            {
                case ContextLookupMode.Nearest:
                    return GetNearestProviderInParents();

                case ContextLookupMode.ByNameInParents:
                    return FindProviderInParentsByName(contextName);

                case ContextLookupMode.ByNameInScene:
                    return FindProviderInSceneByName(contextName);

                default:
                    return GetNearestProviderInParents();
            }
        }

        IBindingContextProvider GetNearestProviderInParents()
        {
            var parents = GetComponentsInParent<MonoBehaviour>(includeInactive: true);
            foreach (var mb in parents)
            {
                if (mb is IBindingContextProvider p)
                    return p;
            }

            return null;
        }

        IBindingContextProvider FindProviderInParentsByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var parents = GetComponentsInParent<MonoBehaviour>(includeInactive: true);
            foreach (var mb in parents)
            {
                if (mb is IBindingContextProvider p)
                {
                    if (mb is MonoContextHolder dc && dc.ContextName == name)
                        return p;

                    if (mb.gameObject.name == name)
                        return p;
                }
            }

            return null;
        }

        IBindingContextProvider FindProviderInSceneByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var all = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var mb in all)
            {
                if (mb is IBindingContextProvider p)
                {
                    if (mb is MonoContextHolder dc && dc.ContextName == name)
                        return p;

                    if (mb.gameObject.name == name)
                        return p;
                }
            }

            return null;
        }
    }
}
