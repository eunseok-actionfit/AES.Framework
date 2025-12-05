// ContextListenerBase.cs
using System.Collections;
using UnityEngine;

namespace AES.Tools
{
    /// <summary>
    /// memberPath 없이 "컨텍스트만" 필요로 하는 바인딩용 베이스.
    /// - Provider 찾기 + RuntimeContext 대기만 공통 처리
    /// - 실제 Subscribe/Unsubscribe는 파생 클래스에서 구현
    /// </summary>
    public abstract class ContextListenerBase : BindingBehaviour
    {
        [SerializeField] ContextLookupMode lookupMode = ContextLookupMode.Nearest;
        [SerializeField] string contextName;

        protected IBindingContextProvider CurrentProvider { get; private set; }
        protected IBindingContext BindingContext => CurrentProvider?.RuntimeContext;

        protected override void Subscribe()
        {
#if UNITY_EDITOR
            Debug_ClearRuntimeInfo();
#endif
            CurrentProvider = ResolveProvider();

#if UNITY_EDITOR
            Debug_OnSubscribeStart(CurrentProvider, null);
#endif

            if (CurrentProvider == null)
            {
                LogBindingError("IBindingContextProvider 를 찾지 못했습니다.");
                return;
            }

            if (BindingContext == null)
            {
                StartCoroutine(WaitForContextAndSubscribe());
                return;
            }

#if UNITY_EDITOR
            Debug_OnContextReady(BindingContext);
#endif

            OnContextAvailable(BindingContext);
        }

        private IEnumerator WaitForContextAndSubscribe()
        {
            while (BindingContext == null)
                yield return null;

#if UNITY_EDITOR
            Debug_OnContextReady(BindingContext);
#endif

            OnContextAvailable(BindingContext);
        }

        protected override void Unsubscribe()
        {
            OnContextUnavailable();
        }

        protected abstract void OnContextAvailable(IBindingContext context);
        protected abstract void OnContextUnavailable();

        // 아래 Provider 찾기 로직은 ContextBindingBase와 동일
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

#if UNITY_2022_2_OR_NEWER
            var all = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
            var all = FindObjectsOfType<MonoBehaviour>(true);
#endif
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
