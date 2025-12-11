using System.Threading;
using AES.Tools.UI.Core.Factory;
using AES.Tools.UI.Core.Registry;
using AES.Tools.UI.Core.View;
using AES.Tools.UI.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.UI.Managers
{
    public sealed class ToastManager : Singleton<ToastManager>
    {
        [SerializeField] Transform    toastRoot;
        [SerializeField] UIRegistrySO uiRegistry;

        [SerializeField] float defaultDuration     = 2f;

        private IUIFactory _factory = new UIFactory();

        public ToastService Service { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Service = new ToastService(
                uiRegistry,
                _factory,
                toastRoot,
                defaultDuration);
        }

        public UniTask<T> ShowAsync<T>(object vm = null, float dur = -1f, CancellationToken ct = default)
            where T : ToastViewBase
            => Service.ShowAsync<T>(vm, dur, ct);

        public void ClearQueue() => Service.ClearQueue();
        public void ClearPool()  => Service.ClearPool();
    }
}