using System;
using UnityEngine;

namespace AES.Tools
{
    public enum MemberPathMode
    {
        Dropdown,   // 드롭다운에서 선택
        Custom      // 수동 문자열 입력
    }

    // ContextBindingBase: DataContext + Path 기반 공통 베이스
    public abstract class ContextBindingBase : BindingBehaviour
    {
        #region Debug
#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        string _resolvedContextName;

        [SerializeField, HideInInspector]
        string _resolvedPath;

        [SerializeField, HideInInspector]
        string _lastValuePreview;
#endif
        #endregion

        [Header("Context")]
        [SerializeField]
        ContextLookupMode lookupMode = ContextLookupMode.Nearest;

        [SerializeField]
        [ShowIf("lookupMode", ContextLookupMode.Nearest, Condition = ShowIfCondition.NotEquals)]
        string contextName;

        [Header("Member Path")]
        [SerializeField]
        MemberPathMode memberPathMode = MemberPathMode.Dropdown;

        // Custom 모드일 때만 인스펙터에 노출 (Dropdown 모드에선 에디터 스크립트가 채워줌)
        [SerializeField]
        [ShowIf("memberPathMode", MemberPathMode.Custom, Condition = ShowIfCondition.Equals)]
        protected string memberPath;

        DataContextBase _context;
        MemberPath _path;

        protected DataContextBase Context
        {
            get
            {
                if (_context == null)
                    _context = ResolveContext();
                return _context;
            }
        }

        protected MemberPath Path
        {
            get
            {
                if (_path == null && Context != null && Context.ViewModel != null && !string.IsNullOrEmpty(memberPath))
                {
                    var type = Context.ViewModel.GetType();
                    try
                    {
                        _path = MemberPathCache.Get(type, memberPath);
                    }
                    catch (Exception ex)
                    {
                        LogBindingException($"memberPath '{memberPath}' 해석 실패", ex);
                    }
                }

                return _path;
            }
        }

        DataContextBase ResolveContext()
        {
            switch (lookupMode)
            {
                case ContextLookupMode.Nearest:
                    return GetComponentInParent<DataContextBase>();

                case ContextLookupMode.ByNameInParents:
                    return FindContextInParentsByName();

                case ContextLookupMode.ByNameInScene:
                    return FindContextInSceneByName();

                default:
                    return GetComponentInParent<DataContextBase>();
            }
        }

        DataContextBase FindContextInParentsByName()
        {
            if (string.IsNullOrEmpty(contextName))
            {
                Debug.LogError($"[{GetType().Name}] lookupMode=ByNameInParents 이지만 contextName 이 비어 있습니다.", this);
                return null;
            }

            var all = GetComponentsInParent<DataContextBase>(includeInactive: true);
            foreach (var ctx in all)
            {
                if (ctx != null && ctx.ContextName == contextName)
                    return ctx;
            }

            Debug.LogError($"[{GetType().Name}] 부모 계층에서 이름 '{contextName}' 인 DataContextBase 를 찾지 못했습니다.", this);
            return null;
        }

        DataContextBase FindContextInSceneByName()
        {
            if (string.IsNullOrEmpty(contextName))
            {
                Debug.LogError($"[{GetType().Name}] lookupMode=ByNameInScene 이지만 contextName 이 비어 있습니다.", this);
                return null;
            }

            var all = FindObjectsByType<DataContextBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ctx in all)
            {
                if (ctx != null && ctx.ContextName == contextName)
                    return ctx;
            }

            Debug.LogError($"[{GetType().Name}] 씬 전체에서 이름 '{contextName}' 인 DataContextBase 를 찾지 못했습니다.", this);
            return null;
        }

        /// <summary>
        /// Context + Path 해석 공통 진입점.
        /// 실패 시 false 반환 + 내부에서 로그 출력.
        /// </summary>
        protected bool TryResolvePath(out object viewModel, out MemberPath path)
        {
            viewModel = null;
            path = null;

            if (Context == null)
            {
                LogBindingError("상위 DataContextBase 를 찾지 못했습니다.");
                return false;
            }

            viewModel = Context.ViewModel;
            if (viewModel == null)
            {
                LogBindingError("Context.ViewModel 이 null 입니다.");
                return false;
            }

            if (string.IsNullOrEmpty(memberPath))
            {
                LogBindingError("memberPath 가 비어 있습니다.");
                return false;
            }

            if (Path == null)
            {
                // Path 프로퍼티 안에서 이미 에러 로그를 찍기 때문에 여기서는 false만 리턴
                return false;
            }

            path = Path;
            return true;
        }

        protected ObservableProperty<T> ResolveObservableProperty<T>()
        {
            Debug_ClearRuntimeInfo();

            if (!TryResolvePath(out var vm, out var path))
                return null;

#if UNITY_EDITOR
            Debug_SetContextAndPath(Context, memberPath);
#endif

            var value = path.GetValue(vm);
            if (value is ObservableProperty<T> prop)
                return prop;

            Debug.LogError($"멤버 '{memberPath}' 는 ObservableProperty<{typeof(T).Name}> 가 아닙니다.", this);
            return null;
        }

        /// <summary>
        /// IObservableProperty (boxing 허용) 해석.
        /// </summary>
        protected IObservableProperty ResolveObservablePropertyBoxed()
        {
            Debug_ClearRuntimeInfo();

            if (!TryResolvePath(out var vm, out var path))
                return null;

#if UNITY_EDITOR
            Debug_SetContextAndPath(Context, memberPath);
#endif

            var value = path.GetValue(vm);
            if (value is IObservableProperty prop)
                return prop;

            LogBindingError($"멤버 '{memberPath}' 는 IObservableProperty 가 아닙니다. 실제 타입: {value?.GetType().Name ?? "null"}");
            return null;
        }

        /// <summary>
        /// IObservableList 해석.
        /// </summary>
        protected IObservableList ResolveObservableList()
        {
            Debug_ClearRuntimeInfo();

            if (!TryResolvePath(out var vm, out var path))
                return null;

#if UNITY_EDITOR
            Debug_SetContextAndPath(Context, memberPath);
#endif

            var value = path.GetValue(vm);
            if (value is IObservableList list)
                return list;

            LogBindingError($"멤버 '{memberPath}' 는 IObservableList 가 아닙니다. 실제 타입: {value?.GetType().Name ?? "null"}");
            return null;
        }
    }
}
