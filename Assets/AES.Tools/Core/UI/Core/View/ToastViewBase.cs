using UnityEngine;


namespace AES.Tools.UI.Core.View
{
    [RequireComponent(typeof(MonoContext))]
    public abstract class ToastViewBase : UIView
    {
        // ToastManager 와의 연동, 자동 Hide 타이머 등은 여기서 구현.
        
        public virtual void BindModelObject(object viewModel)
        {
            var ctx = GetComponent<MonoContext>();
            if (ctx != null)
                ctx.SetViewModel(viewModel);
        }
    }
}


