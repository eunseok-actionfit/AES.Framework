using System;
using System.Collections;
using System.Reflection;
using UnityEngine;


namespace AES.Tools
{
    public enum ViewModelSourceMode { AutoCreate, External, InheritFromParent }

    public enum ContextNameMode { TypeName, GameObjectName, Custom }

    /// <summary>
    /// 모든 ViewModel 컨텍스트 제공자.
    /// 제네릭 없이 하나로 사용한다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MonoContext : MonoBehaviour, IBindingContextProvider
    {
        [Header("Context Name")]
        [SerializeField]
        private ContextNameMode nameMode = ContextNameMode.TypeName;

        [SerializeField]
        [ShowIf(nameof(nameMode), ContextNameMode.Custom)]
        private string customName;

        [Header("ViewModel 생성 방식")]
        [SerializeField]
        private ViewModelSourceMode viewModelSource = ViewModelSourceMode.External;

        // AutoCreate용 타입 정보
        [SerializeField] private string viewModelTypeName;

        [Header("Inherit From Parent Settings")]
        [SerializeField] private ContextLookupMode inheritLookupMode = ContextLookupMode.Nearest;
        [SerializeField] private string inheritContextName; // ByName 모드일 때 사용할 이름
        [SerializeField] private string inheritMemberPath; // 부모 VM 안에서 상속할 베이스 경로 (예: "ChildVm", "Child1.Value")

        private readonly DataContext _dataContext = new();

        // TODO: inheritMemberPath → PropertyPath 변환 적용 고려
        // // 현재는 문자열 기반 경로를 그대로 사용하지만,
        // // 바인딩 시스템 v2 계획 시 PropertyPath로 통합 가능.
        private IBindingContext _inheritRuntimeContext;

        public string ContextName
        {
            get
            {
                switch (nameMode)
                {
                    case ContextNameMode.TypeName:
                        if (ViewModelType == null && !string.IsNullOrEmpty(viewModelTypeName))
                        {
                            TryResolveType();
                        }

                        return ViewModelType != null ? ViewModelType.Name : gameObject.name;

                    case ContextNameMode.Custom:
                        return string.IsNullOrEmpty(customName) ? gameObject.name : customName;

                    default:
                        return gameObject.name;
                }
            }
        }


        public Type ViewModelType { get; private set; }
        public object ViewModel { get; private set; }

        
        [ThreadStatic]
        static int _inheritResolveDepth;
        /// <summary>
        /// 런타임 바인딩 컨텍스트.
        /// - AutoCreate / External: 내부 DataContext
        /// - InheritFromParent: 부모 컨텍스트 + inheritMemberPath 를 래핑한 SubContext
        /// </summary>
        public IBindingContext RuntimeContext
        {
            get
            {
                if (viewModelSource == ViewModelSourceMode.InheritFromParent)
                {
                    if (_inheritRuntimeContext != null)
                        return _inheritRuntimeContext;

                    // 재귀 깊이 가드
                    if (_inheritResolveDepth > 64)
                    {
                        Debug.LogError(
                            "[MonoContext] InheritFromParent RuntimeContext 해석 중 순환 참조 또는 " +
                            "비정상적으로 깊은 상속 체인이 감지되었습니다. " +
                            "inheritLookupMode / inheritContextName 설정을 확인하세요.",
                            this);
                        return null;
                    }

                    _inheritResolveDepth++;
                    try
                    {
                        var parent = ResolveParentProvider();
                        if (parent == null)
                            return null;

                        var parentCtx = parent.RuntimeContext;
                        if (parentCtx == null)
                            return null;

                        var basePath = string.IsNullOrEmpty(inheritMemberPath) ? null : inheritMemberPath;
                        _inheritRuntimeContext = new SubBindingContext(parentCtx, basePath);

                        return _inheritRuntimeContext;
                    }
                    finally
                    {
                        _inheritResolveDepth--;
                    }
                }

                // AutoCreate / External
                return _dataContext.BindingContext;
            }
        }



#if UNITY_EDITOR
        /// <summary>
        /// 에디터 드롭다운용 ViewModel 타입.
        /// - 일반 모드: ViewModelType
        /// - 상속 모드: 부모 타입 + inheritMemberPath 기준으로 계산된 서브 타입
        /// </summary>
        public Type DesignTimeViewModelType
        {
            get
            {
                // 일반 모드
                if (viewModelSource != ViewModelSourceMode.InheritFromParent)
                {
                    if (ViewModelType != null)
                        return ViewModelType;

                    TryResolveType();
                    return ViewModelType;
                }

                // 상속 모드: 부모 + 경로 기준 타입
                var parent = EditorResolveParentProvider();
                if (parent == null)
                    return null;

                var parentType = parent.DesignTimeViewModelType;
                if (parentType == null)
                    return null;

                if (string.IsNullOrEmpty(inheritMemberPath))
                    return parentType;

                return GetTypeFromPath(parentType, inheritMemberPath);
            }
        }

        public object GetDesignTimeViewModel() => null;

        private void OnValidate()
        {
            if (!Application.isPlaying)
                TryResolveType();
        }

        IBindingContextProvider EditorResolveParentProvider()
        {
            switch (inheritLookupMode)
            {
                case ContextLookupMode.Nearest:
                    return GetNearestProviderInParents(excludeSelf: true);

                case ContextLookupMode.ByNameInParents:
                    return FindProviderInParentsByName(inheritContextName, excludeSelf: true);

                case ContextLookupMode.ByNameInScene:
                    return FindProviderInSceneByName(inheritContextName, excludeSelf: true);

                default:
                    return GetNearestProviderInParents(excludeSelf: true);
            }
        }

        Type GetTypeFromPath(Type rootType, string path)
        {
            if (rootType == null || string.IsNullOrEmpty(path))
                return rootType;

            var parts = path.Split('.');
            var t = rootType;

            foreach (var part in parts)
            {

                var prop = t.GetProperty(part,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (prop != null)
                {
                    t = prop.PropertyType;
                    continue;
                }

                var field = t.GetField(part,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                {
                    t = field.FieldType;
                    continue;
                }

                // 찾지 못하면 실패
                return null;
            }

            return t;
        }
#endif

        private void Awake()
        {
            switch (viewModelSource)
            {
                case ViewModelSourceMode.AutoCreate:
                    TryAutoCreateViewModel();
                    break;

                case ViewModelSourceMode.InheritFromParent:
                    // 예전: StartCoroutine(CoInitInheritFromParent());
                    // 이제는 RuntimeContext 게터에서 필요할 때 상속 컨텍스트를 만든다.
                    break;

                case ViewModelSourceMode.External:
                default:
                    // 외부에서 SetViewModel() 호출
                    break;
            }
        }


        private void TryAutoCreateViewModel()
        {
            TryResolveType();

            if (ViewModelType == null)
                return;

            try
            {
                var vm = Activator.CreateInstance(ViewModelType);
                SetViewModel(vm);
            }
            catch (Exception e) { Debug.LogError($"[MonoContext] ViewModel 인스턴스 생성 중 예외 발생: {e}", this); }
        }

        /// <summary>
        /// InheritFromParent 모드 초기화.
        /// 부모 RuntimeContext 가 준비될 때까지 기다렸다가 SubBindingContext 생성.
        /// </summary>
        /// <summary>
        /// InheritFromParent 모드 초기화.
        /// 부모 RuntimeContext 가 준비될 때까지 기다렸다가 SubBindingContext 생성.
        /// </summary>
// MonoContext.cs
        private IEnumerator CoInitInheritFromParent()
        {
            var parent = ResolveParentProvider();

            if (parent == null)
            {
                Debug.LogWarning("[MonoContext] 상속용 부모 IBindingContextProvider 를 찾지 못했습니다.", this);
                yield break;
            }

            // 부모 RuntimeContext 준비될 때까지 대기
            IBindingContext parentCtx = null;

            while ((parentCtx = parent.RuntimeContext) == null)
            {
                Debug.Log(
                    $"[MonoContext:{name}] InheritFromParent → 부모 RuntimeContext 아직 null. " +
                    $"parent={(parent as MonoBehaviour)?.name}",
                    this);

                yield return null;
            }

            var basePath = string.IsNullOrEmpty(inheritMemberPath) ? null : inheritMemberPath;
            _inheritRuntimeContext = new SubBindingContext(parentCtx, basePath);

            Debug.Log(
                $"[MonoContext:{name}] InheritFromParent 초기화 완료. " +
                $"Parent={(parent as MonoBehaviour)?.name}, BasePath={basePath}",
                this);
        }



        private IBindingContextProvider ResolveParentProvider()
        {
            switch (inheritLookupMode)
            {
                case ContextLookupMode.Nearest:
                    return GetNearestProviderInParents(excludeSelf: true);

                case ContextLookupMode.ByNameInParents:
                    return FindProviderInParentsByName(inheritContextName, excludeSelf: true);

                case ContextLookupMode.ByNameInScene:
                    return FindProviderInSceneByName(inheritContextName, excludeSelf: true);

                default:
                    return GetNearestProviderInParents(excludeSelf: true);
            }
        }

        private IBindingContextProvider GetNearestProviderInParents(bool excludeSelf)
        {
            var parents = GetComponentsInParent<MonoBehaviour>(includeInactive: true);

            foreach (var mb in parents)
            {
                if (excludeSelf && mb == this)
                    continue;

                if (mb is IBindingContextProvider p)
                    return p;
            }

            return null;
        }

        private IBindingContextProvider FindProviderInParentsByName(string name, bool excludeSelf)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var parents = GetComponentsInParent<MonoBehaviour>(includeInactive: true);

            foreach (var mb in parents)
            {
                if (excludeSelf && mb == this)
                    continue;

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

        private IBindingContextProvider FindProviderInSceneByName(string name, bool excludeSelf)
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
                if (excludeSelf && mb == this)
                    continue;

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

        private void TryResolveType()
        {
            if (string.IsNullOrEmpty(viewModelTypeName))
            {
                ViewModelType = null;
                return;
            }

            var type = Type.GetType(viewModelTypeName);

            if (type == null)
            {
                Debug.LogWarning($"[MonoContext] ViewModel 타입을 찾을 수 없습니다: {viewModelTypeName}", this);
                ViewModelType = null;
                return;
            }

            ViewModelType = type;
        }

        /// <summary>
        /// Presenter/Service/Bootstrap에서 ViewModel을 주입할 때 사용한다.
        /// </summary>
        public void SetViewModel(object viewModel, bool updateType = true)
        {
            ViewModel = viewModel;

            if (updateType && ViewModel != null)
                ViewModelType = ViewModel.GetType();

            _dataContext.SetViewModel(ViewModel);
        }

        /// <summary>
        /// 부모 컨텍스트 + basePath 를 래핑하는 서브 컨텍스트.
        /// </summary>
        private sealed class SubBindingContext : IBindingContext
        {
            private readonly IBindingContext _parent;
            private readonly string _basePath;

            public SubBindingContext(IBindingContext parent, string basePath)
            {
                _parent = parent ?? throw new ArgumentNullException(nameof(parent));
                _basePath = string.IsNullOrEmpty(basePath) ? null : basePath;
            }

            // TODO: 문자열 경로 결합 대신 PropertyPath 기반으로 리팩터링 고려
            // 현재는 "a.b" 방식 문자열 경로를 그대로 위임함.
            private string Concat(string path)
            {
                if (string.IsNullOrEmpty(_basePath))
                    return path ?? string.Empty;

                if (string.IsNullOrEmpty(path))
                    return _basePath;

                return _basePath + "." + path;
            }

            public object GetValue()
            {
                if (string.IsNullOrEmpty(_basePath))
                    return _parent.GetValue();

                return _parent.GetValue(_basePath);
            }

            public object GetValue(string path)
            {
                var full = Concat(path);
                return _parent.GetValue(full);
            }

            public void SetValue(string path, object value)
            {
                var full = Concat(path);
                _parent.SetValue(full, value);
            }

            public object RegisterListener(string path, Action<object> onValueChanged, bool pushInitialValue = true)
            {
                var full = Concat(path);
                return _parent.RegisterListener(full, onValueChanged, pushInitialValue);
            }

            public void RemoveListener(string path, object token = null)
            {
                var full = Concat(path);
                _parent.RemoveListener(full, token);
            }
        }
    }
}