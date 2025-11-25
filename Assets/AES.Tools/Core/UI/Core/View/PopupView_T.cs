using System;


namespace AES.Tools.View
{
 
    public abstract class PopupViewBase<TViewModel> : PopupViewBase
        where TViewModel : class
    {
        readonly DataContext _ctx = new ();

        public override Type ViewModelType => typeof(TViewModel);

        public override IBindingContext RuntimeContext => _ctx.BindingContext;

#if UNITY_EDITOR
        public override Type DesignTimeViewModelType => typeof(TViewModel);
        public override object GetDesignTimeViewModel() => null;
#endif

        public void Bind(TViewModel vm)
        {
            _ctx.SetViewModel(vm);
            OnBind(vm);
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
                $"PopupView<{typeof(TViewModel).Name}> 는 {typeof(TViewModel).Name} 타입의 모델만 받습니다. " +
                $"실제 타입: {model.GetType().Name}");
        }

        protected virtual void OnBind(TViewModel vm) { }
    }
}


