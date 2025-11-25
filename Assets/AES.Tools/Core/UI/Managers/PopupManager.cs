using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace AES.Tools
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