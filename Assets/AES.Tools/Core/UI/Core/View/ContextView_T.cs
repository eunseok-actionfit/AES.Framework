using System;


namespace AES.Tools.View
{
    public abstract class ContextViewBase<TViewModel> : ContextViewBase
        where TViewModel : class 
    {
        readonly DataContext _ctx = new DataContext();

        public override Type ViewModelType => typeof(TViewModel);

        public override IBindingContext RuntimeContext => _ctx.BindingContext;

#if UNITY_EDITOR
        public override Type DesignTimeViewModelType => typeof(TViewModel);
        public override object GetDesignTimeViewModel() => null;
#endif

        public void Bind(TViewModel viewModel)
        {
            _ctx.SetViewModel(viewModel);
            OnBind(viewModel);
        }

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
                $"ContextView<{typeof(TViewModel).Name}> 은 {typeof(TViewModel).Name} 모델만 받습니다. (입력: {model.GetType().Name})");
        }

        protected virtual void OnBind(TViewModel vm) { }
    }
}


