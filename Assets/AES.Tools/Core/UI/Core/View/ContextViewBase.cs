using System;


namespace AES.Tools.View
{
    public abstract class ContextViewBase : UIView, IBindingContextProvider
    {
        public abstract Type ViewModelType { get; }
        public abstract IBindingContext RuntimeContext { get; }

#if UNITY_EDITOR
        public abstract Type   DesignTimeViewModelType { get; }
        public abstract object GetDesignTimeViewModel();
#endif

        public abstract void BindModelObject(object model);
    }
    
    
}