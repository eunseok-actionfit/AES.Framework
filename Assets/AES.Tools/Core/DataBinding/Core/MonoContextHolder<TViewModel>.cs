using System;

namespace AES.Tools
{
    public abstract class MonoContext<TViewModel> : MonoContextHolder
        where TViewModel : class
    {
        public TViewModel ViewModelTyped => (TViewModel)ViewModel;

        public override Type ViewModelType => typeof(TViewModel);

        protected sealed override object CreateViewModel()
        {
            return CreateViewModelTyped();
        }

        protected abstract TViewModel CreateViewModelTyped();

        public void SetViewModel(TViewModel vm, bool recreateContext = true)
        {
            base.SetViewModel(vm, recreateContext);
        }
    }
}