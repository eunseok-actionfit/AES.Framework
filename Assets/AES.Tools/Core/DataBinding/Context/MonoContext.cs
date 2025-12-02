using System;
using UnityEngine;


namespace AES.Tools
{
    public enum ViewModelSourceMode { AutoCreate, External,   InheritFromParent }

    public enum ContextNameMode { TypeName, GameObjectName, Custom }

    /// <summary>
    /// 모든 ViewModel 컨텍스트 제공자.
    /// 제네릭 없이 하나로 사용한다.
    /// </summary>
    [DefaultExecutionOrder(-10)]
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

        // AutoCreate용 타입 정보 (원하면 안 써도 됨)
        [SerializeField] private string viewModelTypeName;
        
        [Header("Inherit From Parent Settings")]
        [SerializeField] private ContextLookupMode inheritLookupMode = ContextLookupMode.Nearest;
        [SerializeField] private string inheritContextName;  // ByName 모드일 때 쓸 이름
        [SerializeField] private string inheritMemberPath;   // 부모 VM 안에서 자식 VM 위치 (예: "ChildVm")


        private readonly DataContext _dataContext = new DataContext();

        public string ContextName
        {
            get
            {
                switch (nameMode)
                {
                    case ContextNameMode.TypeName:
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

        public IBindingContext RuntimeContext => _dataContext.BindingContext;

#if UNITY_EDITOR
        public Type DesignTimeViewModelType
        {
            get
            {
                if (ViewModelType != null)
                    return ViewModelType;

                TryResolveType();
                return ViewModelType;
            }
        }

        public object GetDesignTimeViewModel() => null;

        private void OnValidate()
        {
            if (!Application.isPlaying)
                TryResolveType();
        }
#endif

        private void Awake()
        {
            if (viewModelSource == ViewModelSourceMode.AutoCreate)
            {
                TryResolveType();

                if (ViewModelType != null)
                {
                    var vm = Activator.CreateInstance(ViewModelType);
                    SetViewModel(vm);
                }
            }
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
    }
}