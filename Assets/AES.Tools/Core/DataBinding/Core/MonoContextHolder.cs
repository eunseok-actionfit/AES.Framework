using System;
using System.ComponentModel;
using UnityEngine;

namespace AES.Tools
{
    public enum ViewModelSourceMode
    {
        AutoCreate,
        External
    }
    
    public enum ContextNameMode
    {
        TypeName,
        GameObjectName,
        Custom
    }

    [DefaultExecutionOrder(-1)]
    public abstract class MonoContextHolder : MonoBehaviour, IBindingContextProvider
    {
        [Header("Context Name")]
        [SerializeField]
        private ContextNameMode nameMode = ContextNameMode.TypeName;

        [SerializeField]
        [ShowIf(nameof(nameMode), ContextNameMode.Custom)]
        private string customName;

        [Header("ViewModel 생성 방식")]
        [SerializeField]
        private ViewModelSourceMode viewModelSource = ViewModelSourceMode.AutoCreate;

        // ============================================================
        //  Public Properties
        // ============================================================

        public string ContextName
        {
            get
            {
                switch (nameMode)
                {
                    case ContextNameMode.TypeName:
                        return GetType().Name;

                    case ContextNameMode.Custom:
                        return string.IsNullOrEmpty(customName)
                            ? gameObject.name
                            : customName;

                    default:
                        return gameObject.name;
                }
            }
        }

        /// <summary>이 Context가 다루는 ViewModel 타입 (Editor/Runtime 공용)</summary>
        public abstract Type ViewModelType { get; }

        /// <summary>실제 ViewModel 인스턴스</summary>
        public object ViewModel { get; private set; }

        /// <summary>순수 C# 계층의 Context (Observable or INPC)</summary>
        public ViewModelContext ViewModelContext { get; private set; }

        /// <summary>바인딩에서 실제 사용되는 추상화된 컨텍스트</summary>
        public IBindingContext BindingContext => ViewModelContext;

        // IBindingContextProvider 구현 -------------------------------

        public IBindingContext RuntimeContext => BindingContext;

#if UNITY_EDITOR
        public virtual Type DesignTimeViewModelType => ViewModelType;

        public virtual object GetDesignTimeViewModel() => CreateDesignTimeViewModel();
#endif

        // ============================================================
        //  Lifecycle
        // ============================================================

        protected virtual void Awake()
        {
            EnsureViewModelCreated();
            EnsureContextCreated();
        }

        private void EnsureViewModelCreated()
        {
            if (ViewModel != null)
                return;

            if (viewModelSource == ViewModelSourceMode.AutoCreate)
            {
                ViewModel = CreateViewModel();
            }
        }

        private void EnsureContextCreated()
        {
            if (ViewModel == null)
                return;

            if (ViewModelContext == null)
                ViewModelContext = CreateViewModelContext(ViewModel);
        }

        // ============================================================
        //  Public API
        // ============================================================

        public void SetViewModel(object viewModel, bool recreateContext = true)
        {
            ViewModel = viewModel;

            if (!recreateContext)
                return;

            ViewModelContext = viewModel != null
                ? CreateViewModelContext(viewModel)
                : null;
        }

        // ============================================================
        //  Abstract
        // ============================================================

        /// <summary>
        /// AutoCreate 모드에서 사용할 ViewModel 생성 로직.
        /// </summary>
        protected abstract object CreateViewModel();

        // ============================================================
        //  Context Factory
        // ============================================================

        protected virtual ViewModelContext CreateViewModelContext(object viewModel)
        {
            if (viewModel is INotifyPropertyChanged inpc)
                return new NotifyPropertyChangedViewModelContext(inpc);

            return new ObservableViewModelContext(viewModel);
        }

        // ============================================================
        //  Editor Helpers
        // ============================================================

#if UNITY_EDITOR
        /// <summary>
        /// 디자인타임 전용 ViewModel 인스턴스가 필요하면 오버라이드해서 생성.
        /// 기본값은 null 이며, 이 경우 타입 정보만으로 드롭다운을 구성한다.
        /// </summary>
        protected virtual object CreateDesignTimeViewModel()
        {
            return null;
        }
#endif
    }
}
