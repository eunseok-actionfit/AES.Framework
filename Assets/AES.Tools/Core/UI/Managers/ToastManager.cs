using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace AES.Tools
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