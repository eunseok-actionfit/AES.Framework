using System.Threading;
using AES.Tools.UI.Core.Factory;
using AES.Tools.UI.Core.Registry;
using AES.Tools.UI.Core.View;
using AES.Tools.UI.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.UI.Managers
{
    public sealed class PopupManager : Singleton<PopupManager>
    {
        [SerializeField] Transform    popupRoot;
        [SerializeField] UIRegistrySO uiRegistry;

        IUIFactory _factory = new UIFactory();

        public PopupService Service { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Service = new PopupService(uiRegistry, _factory, popupRoot);
        }

        public UniTask<T> EnqueueAsync<T>(object vm = null, CancellationToken ct = default)
            where T : PopupViewBase
            => Service.EnqueueAsync<T>(vm, ct);

        public UniTask<T> PushImmediateAsync<T>(object vm = null, CancellationToken ct = default)
            where T : PopupViewBase
            => Service.PushImmediateAsync<T>(vm, ct);

        public void CloseTop() => Service.CloseTop();
        public void ClearAll() => Service.ClearAll();
        public void OnPopupClosed(PopupViewBase view) => Service.OnPopupClosed(view);
    }
}