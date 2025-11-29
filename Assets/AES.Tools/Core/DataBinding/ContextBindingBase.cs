using System.Collections;
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

            if (CurrentProvider == null)
            {
                LogBindingError("IBindingContextProvider를 찾지 못했습니다.");
                return;
            }

            // 여기에서 RuntimeContext가 아직 null이면, 한동안 기다렸다가 다시 시도
            if (BindingContext == null)
            {
                // 지연 구독
                StartCoroutine(WaitForContextAndSubscribe());
                return;
            }

            OnContextAvailable(BindingContext, memberPath);
        }

        private IEnumerator WaitForContextAndSubscribe()
        {
            // CurrentProvider는 이미 찾았으니, 그 RuntimeContext가 생길 때까지 대기
            while (BindingContext == null)
                yield return null;

#if UNITY_EDITOR
            Debug_SetContextAndPath(CurrentProvider as Object, memberPath);
#endif

            OnContextAvailable(BindingContext, memberPath);
        }


        protected override void Unsubscribe()
        {
            OnContextUnavailable();
        }
        
        protected TViewModel GetViewModel<TViewModel>()
            where TViewModel : class
        {
            return BindingContext?.GetValue() as TViewModel;
        }

        protected T GetValue<T>(string path = null)
        {
            var value = BindingContext?.GetValue(path);
            return value is T t ? t : default;
        }

        protected void SetValue(string path, object value)
        {
            BindingContext?.SetValue(path, value);
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
                    if (mb is MonoContext dc && dc.ContextName == name)
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
                    if (mb is MonoContext dc && dc.ContextName == name)
                        return p;

                    if (mb.gameObject.name == name)
                        return p;
                }
            }

            return null;
        }
    }
}
