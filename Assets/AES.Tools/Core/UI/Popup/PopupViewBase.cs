

using System;


namespace AES.Tools.View
{
    /// <summary>
    /// 모든 팝업 View 의 베이스. IBindingContextProvider 를 구현해서
    /// 자식 ContextBindingBase 들이 이 뷰의 컨텍스트를 자동으로 찾을 수 있게 한다.
    /// </summary>
    public abstract class PopupViewBase : UIView, IBindingContextProvider
    {
        /// <summary>
        /// 이 뷰가 사용하는 ViewModel 타입 (없으면 null 허용)
        /// </summary>
        public abstract Type ViewModelType { get; }

        /// <summary>
        /// 런타임 바인딩 컨텍스트 (PureDataContext → ViewModelContext → IBindingContext)
        /// </summary>
        public abstract IBindingContext RuntimeContext { get; }

#if UNITY_EDITOR
        public abstract Type  DesignTimeViewModelType { get; }
        public abstract object GetDesignTimeViewModel();
#endif

        /// <summary>
        /// PopupManager 가 object 기반으로 모델을 넘길 때 사용하는 진입점.
        /// 제네릭 파생 클래스에서 타입 체크 후 실제 Bind 로 연결.
        /// </summary>
        public abstract void BindModelObject(object model);
    }

    /// <summary>
    /// TViewModel 타입의 ViewModel을 사용하는 팝업 뷰.
    /// PureDataContext 를 내부에 들고 있고, IBindingContextProvider 역할을 한다.
    /// </summary>
    public abstract class PopupView<TViewModel> : PopupViewBase
        where TViewModel : class
    {
        readonly DataContext _ctx = new();

        public override Type ViewModelType => typeof(TViewModel);

        public override IBindingContext RuntimeContext => _ctx.BindingContext;

#if UNITY_EDITOR
        public override Type DesignTimeViewModelType => typeof(TViewModel);

        public override object GetDesignTimeViewModel()
        {
            // 필요하면 테스트용 ViewModel 생성해서 돌려줘도 됨
            return null;
        }
#endif

        /// <summary>
        /// 타입이 맞는 ViewModel을 바인딩.
        /// </summary>
        public void Bind(TViewModel viewModel)
        {
            _ctx.SetViewModel(viewModel);
            OnBind(viewModel);
        }

        /// <summary>
        /// PopupManager 가 object 를 넘길 때 호출되는 래퍼.
        /// 타입이 맞지 않으면 예외를 던진다.
        /// </summary>
        public override void BindModelObject(object model)
        {
            if (model == null)
            {
                Bind(null);
                return;
            }

            if (model is TViewModel typed)
            {
                Bind(typed);
                return;
            }

            throw new ArgumentException(
                $"PopupView<{typeof(TViewModel).Name}> 는 " +
                $"{typeof(TViewModel).Name} 타입의 모델을 기대하지만, " +
                $"{model.GetType().Name} 이 전달되었습니다.");
        }

        /// <summary>
        /// 실제 뷰 초기화/바인딩 로직은 여기서 수행.
        /// </summary>
        protected virtual void OnBind(TViewModel viewModel)
        {
            // 필요 시 override
        }
    }
}
