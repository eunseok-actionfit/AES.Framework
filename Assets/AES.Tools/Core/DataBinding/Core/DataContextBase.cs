using System;
using System.ComponentModel;
using UnityEngine;

namespace AES.Tools
{
    public enum ViewModelSourceMode
    {
        AutoCreate,     // CreateViewModel() 에서 생성 (기존 방식)
        External        // 외부에서 SetViewModel()로 주입
    }
    
    public enum ContextNameMode
    {
        TypeName,
        GameObjectName,
        Custom
    }

    [DefaultExecutionOrder(-1)]
    public abstract class DataContextBase : MonoBehaviour
    {
        [Header("Context Name")]
        [SerializeField]
        ContextNameMode nameMode = ContextNameMode.TypeName;

        [SerializeField]
        [ShowIf(nameof(nameMode), ContextNameMode.Custom)]
        string customName;

        [Header("ViewModel 생성 방식")]
        [SerializeField]
        ViewModelSourceMode viewModelSource = ViewModelSourceMode.AutoCreate;

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

        // ============================================================
        //  Lifecycle
        // ============================================================

        protected virtual void Awake()
        {
            EnsureViewModelCreated();
            EnsureContextCreated();
        }

        // ------------------------------------------------------------

        protected void EnsureViewModelCreated()
        {
            if (ViewModel != null)
                return;

            if (viewModelSource == ViewModelSourceMode.AutoCreate)
            {
                ViewModel = CreateViewModel();
            }
            // External 모드는 외부에서 SetViewModel로 들어옴
        }

        protected void EnsureContextCreated()
        {
            if (ViewModel == null)
                return;

            if (ViewModelContext == null)
                ViewModelContext = CreateViewModelContext(ViewModel);
        }

        // ============================================================
        //  Public API
        // ============================================================

        /// <summary>
        /// 외부에서 ViewModel을 주입할 때 호출.
        /// (예: DI / GameManager / ScriptableInstaller)
        /// </summary>
        public void SetViewModel(object viewModel, bool recreateContext = true)
        {
            ViewModel = viewModel;

            if (recreateContext)
            {
                ViewModelContext = (viewModel != null)
                    ? CreateViewModelContext(viewModel)
                    : null;
            }
        }

        // ============================================================
        //  Abstract
        // ============================================================

        /// <summary>AutoCreate 모드일 경우 ViewModel 생성</summary>
        protected abstract object CreateViewModel();

        // ============================================================
        //  Context Factory
        // ============================================================

        /// <summary>
        /// ViewModel 타입에 따라 올바른 ViewModelContext를 생성
        /// </summary>
        protected virtual ViewModelContext CreateViewModelContext(object viewModel)
        {
            // INPC 기반 ViewModel → NotifyPropertyChangedContext 사용
            if (viewModel is INotifyPropertyChanged inpc)
                return new NotifyPropertyChangedViewModelContext(inpc);

            // 나머지는 네가 이미 만든 ObservableProperty 기반 컨텍스트 적용
            return new ObservableViewModelContext(viewModel);
        }

        // ============================================================
        //  Editor Helpers
        // ============================================================

#if UNITY_EDITOR
        public virtual Type GetViewModelType() => ViewModelType;

        public virtual object GetDesignTimeViewModel()
        {
            // 필요하면 디자인타임용 mock VM 생성
            return null;
        }
#endif
    }
}
