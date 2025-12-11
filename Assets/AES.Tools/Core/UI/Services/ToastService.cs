using System;
using System.Collections.Generic;
using System.Threading;
using AES.Tools.UI.Core.Factory;
using AES.Tools.UI.Core.Registry;
using AES.Tools.UI.Core.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.UI.Services
{
    public sealed class ToastService
    {
        readonly UIRegistrySO _registry;
        readonly IUIFactory   _factory;
        readonly Transform    _toastRoot;

        readonly Queue<ToastRequestBase> _queue = new();
        readonly Dictionary<Type, ObjectPool<UIView>> _pools = new();

        readonly float _defaultDuration;

        bool _isProcessing;

        public ToastService(
            UIRegistrySO registry,
            IUIFactory factory,
            Transform toastRoot,
            float defaultDuration)
        {
            _registry   = registry;
            _factory    = factory;
            _toastRoot  = toastRoot;
            _defaultDuration = defaultDuration;
        }

        // =============================================
        // Public API
        // =============================================

        public UniTask<TView> ShowAsync<TView>(
            object vm = null,
            float duration = -1f,
            CancellationToken ct = default)
            where TView : ToastViewBase
        {
            if (duration <= 0) duration = _defaultDuration;

            var tcs = new UniTaskCompletionSource<TView>();
            _queue.Enqueue(new ToastRequest<TView>(vm, duration, tcs));

            _ = ProcessQueueAsync(ct);
            return tcs.Task;
        }

        public void ClearQueue() => _queue.Clear();

        public void ClearPool()
        {
            foreach (var p in _pools.Values)
                p.Dispose();

            _pools.Clear();
        }

        // =============================================
        // Internal
        // =============================================

        async UniTask ProcessQueueAsync(CancellationToken ct)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            try
            {
                while (_queue.Count > 0 && !ct.IsCancellationRequested)
                {
                    var req = _queue.Dequeue();

                    var (entry, toast) = await req.CreateAndShowAsync(this, ct);
                    if (!toast)
                        continue;

                    await UniTask.Delay(TimeSpan.FromSeconds(req.Duration), cancellationToken: ct);
                    if (ct.IsCancellationRequested)
                        break;

                    toast.Hide();
                    Release(entry, toast);
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        internal async UniTask<TView> CreateAsync<TView>(object vm, CancellationToken ct)
            where TView : ToastViewBase
        {
            var type = typeof(TView);

            if (!_registry.TryGetByViewType(type, out var entry) || entry == null)
                throw new InvalidOperationException($"{type.Name} not found in Registry.");

            // UsePool == false 이면 풀 안 쓰고 곧바로 생성/파괴
            if (!entry.UsePool)
            {
                var uiView = await _factory.CreateAsync(entry, _toastRoot, ct);
                var toast  = uiView.GetComponent<TView>();
                if (!toast)
                    throw new MissingComponentException($"{type.Name} component not found on prefab.");

                toast.BindModelObject(vm);
                toast.Show();

                return toast;
            }

            // 여기부터는 풀 사용
            var pool  = GetPool(type, entry);
            var view  = await pool.Rent(ct);
            var toastFromPool = view.GetComponent<TView>();
            if (!toastFromPool)
                throw new MissingComponentException($"{type.Name} component not found on pooled view.");

            toastFromPool.BindModelObject(vm);
            toastFromPool.Show();

            return toastFromPool;
        }

        void Release(UIRegistryEntry entry, ToastViewBase toast)
        {
            if (!toast)
                return;

            var t = toast.GetComponent<UIView>();
            if (!t)
                return;

            var type = toast.GetType();

            // 해당 타입이 풀을 안 쓰는 설정이라면 그냥 Destroy
            if (!entry.UsePool || !_pools.TryGetValue(type, out var pool))
            {
                _factory.Destroy(entry, t);
                return;
            }

            pool.Return(t);
        }

        ObjectPool<UIView> GetPool(Type type, UIRegistryEntry entry)
        {
            if (_pools.TryGetValue(type, out var pool))
                return pool;

           
            int capacity = entry.Capacity;
            int warm = entry.WarmUp;
            

            var pFactory = new UIPoolFactory(_factory, entry, _toastRoot);

            pool = new ObjectPool<UIView>(
                pFactory,
                capacity,
                onBeforeReturn: v =>
                {
                    if (!v) return;
                    v.gameObject.SetActive(false);
                    v.transform.SetParent(_toastRoot, false);
                },
                onAfterRent: v =>
                {
                    if (!v) return;
                    v.gameObject.SetActive(true);
                    v.transform.SetParent(_toastRoot, false);
                }
            );

            if (warm > 0)
                pool.WarmupAsync(warm).Forget();

            _pools.Add(type, pool);
            return pool;
        }

        // =============================================
        // Request Types
        // =============================================

        abstract class ToastRequestBase
        {
            public float Duration { get; protected set; }

            public abstract UniTask<(UIRegistryEntry entry, ToastViewBase toast)>
                CreateAndShowAsync(ToastService svc, CancellationToken ct);
        }

        sealed class ToastRequest<TView> : ToastRequestBase
            where TView : ToastViewBase
        {
            readonly object _vm;
            readonly UniTaskCompletionSource<TView> _tcs;

            public ToastRequest(object vm, float dur, UniTaskCompletionSource<TView> tcs)
            {
                _vm = vm;
                Duration = dur;
                _tcs = tcs;
            }

            public override async UniTask<(UIRegistryEntry entry, ToastViewBase toast)>
                CreateAndShowAsync(ToastService svc, CancellationToken ct)
            {
                var type = typeof(TView);

                if (!svc._registry.TryGetByViewType(type, out var entry) || entry == null)
                    throw new InvalidOperationException($"ToastView {type.Name} not found in Registry.");

                var toast = await svc.CreateAsync<TView>(_vm, ct);

                _tcs.TrySetResult(toast);
                return (entry, toast);
            }
        }
    }
}
