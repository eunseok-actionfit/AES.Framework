using AES.Tools.UI.Managers;
using UnityEngine;


namespace AES.Tools.UI.Core.View
{
    [RequireComponent(typeof(MonoContext))]
    public abstract class PopupViewBase : UIView
    {
        protected override void Awake()
        {
            base.Awake();
            OnHideCompleted.AddListener(NotifyClosed);
        }

        void NotifyClosed()
        {
            PopupManager.Instance.OnPopupClosed(this);
        }

        /// <summary>
        /// PopupService에서 넘겨주는 viewModel을 MonoContext로 전달.
        /// </summary>
        public virtual void BindModelObject(object viewModel)
        {
            var ctx = GetComponent<MonoContext>();
            if (ctx != null)
                ctx.SetViewModel(viewModel);
        }
    }
}