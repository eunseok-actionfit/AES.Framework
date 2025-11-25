using System;


namespace AES.Tools
{
    
    /// <summary>
    /// TViewModel 을 사용하는 토스트 View.
    /// - 내부에 PureDataContext 를 들고 있고
    /// - IBindingContextProvider 를 통해 바인딩 컨텍스트를 노출한다.
    /// - 실제 UI는 ContextBinding(TextBinding 등)으로 묶으면 된다.
    /// </summary>
    public abstract class ToastViewBase<TViewModel> : ToastViewBase
        where TViewModel : class
    {
        readonly DataContext _ctx = new DataContext();

        public override Type ViewModelType => typeof(TViewModel);

        public override IBindingContext RuntimeContext => _ctx.BindingContext;

#if UNITY_EDITOR
        public override Type DesignTimeViewModelType => typeof(TViewModel);
        public override object GetDesignTimeViewModel() => null;
#endif

        /// <summary>
        /// 타입이 맞는 ViewModel 바인딩.
        /// </summary>
        public void Bind(TViewModel viewModel)
        {
            _ctx.SetViewModel(viewModel);
            OnBind(viewModel);
        }

        /// <summary>
        /// ToastManager 가 object 로 넘겨줄 때 진입점.
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
                $"ToastView<{typeof(TViewModel).Name}> 는 {typeof(TViewModel).Name} 타입의 모델만 받습니다. " +
                $"실제 타입: {model.GetType().Name}");
        }

        /// <summary>
        /// 실제 토스트 UI 초기화/셋업은 여기서.
        /// 내부 Text/Image 등은 ContextBinding 으로 처리하면 된다.
        /// </summary>
        protected virtual void OnBind(TViewModel viewModel) { }
    }
}