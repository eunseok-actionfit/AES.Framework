using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using AES.Tools.View;
using UnityEngine;

namespace AES.Tools
{
    public sealed class PopupService
    {
        readonly UIRegistrySO _registry;
        readonly IUIFactory   _factory;
        readonly Transform    _popupRoot;

        readonly Queue<PopupRequestBase>          _queue = new();
        readonly Stack<ActivePopup>               _stack = new();
        readonly Dictionary<Type, ObjectPool<UIView>> _pools = new();

        bool _isShowing;

        public PopupService(UIRegistrySO registry, IUIFactory factory, Transform popupRoot)
        {
            _registry  = registry;
            _factory   = factory;
            _popupRoot = popupRoot;
        }

        // ======================================================
        // Public API
        // ======================================================

        public UniTask<TView> EnqueueAsync<TView>(
            object viewModel = null,
            CancellationToken ct = default)
            where TView : PopupViewBase
        {
            var tcs  = new UniTaskCompletionSource<TView>();
            var item = new PopupRequest<TView>(viewModel, tcs);

            _queue.Enqueue(item);
            _ = TryShowNextAsync(ct);

            return tcs.Task;
        }

        public async UniTask<TView> PushImmediateAsync<TView>(
            object viewModel = null,
            CancellationToken ct = default)
            where TView : PopupViewBase
        {
            var popup = await CreateAsync<TView>(viewModel, ct);
            popup.Show();

            var entry = GetEntry(typeof(TView));
            _stack.Push(new ActivePopup(entry, popup));

            return popup;
        }

        public void CloseTop()
        {
            if (_stack.Count == 0)
                return;

            var top = _stack.Peek();
            top.View?.Hide();
        }

        public void ClearAll()
        {
            _queue.Clear();

            while (_stack.Count > 0)
            {
                var active = _stack.Pop();
                if (active.View)
                    Release(active.Entry, active.View);
            }
        }

        public void ClearPool()
        {
            foreach (var p in _pools.Values)
                p.Dispose();
            _pools.Clear();
        }

        public void OnPopupClosed(PopupViewBase view)
        {
            if (!view) return;
            if (_stack.Count == 0) return;

            var top = _stack.Peek();
            if (top.View != view) return;

            _stack.Pop();
            Release(top.Entry, top.View);

            _ = TryShowNextAsync();
        }

        // ======================================================
        // Internal
        // ======================================================

        async UniTask TryShowNextAsync(CancellationToken ct = default)
        {
            if (_isShowing)   return;
            if (_stack.Count > 0) return;
            if (_queue.Count == 0) return;

            _isShowing = true;

            try
            {
                var req = _queue.Dequeue();
                var (entry, view) = await req.CreateAndShowAsync(this, ct);
                if (view)
                    _stack.Push(new ActivePopup(entry, view));
            }
            finally
            {
                _isShowing = false;
            }
        }

        internal async UniTask<TView> CreateAsync<TView>(
            object vm, CancellationToken ct)
            where TView : PopupViewBase
        {
            var type  = typeof(TView);
            var entry = GetEntry(type);

            // 풀 미사용이면 바로 생성/파괴
            if (!entry.UsePool)
            {
                var uiView = await _factory.CreateAsync(entry, _popupRoot, ct);
                var popup  = uiView.GetComponent<TView>();
                if (!popup)
                    throw new MissingComponentException($"{type.Name} component not found on prefab.");

                popup.BindModelObject(vm);
                return popup;
            }

            // 풀 사용
            var pool  = GetPool(type, entry);
            var view  = await pool.Rent(ct);
            var popupFromPool = view.GetComponent<TView>();
            if (!popupFromPool)
                throw new MissingComponentException($"{type.Name} component not found on pooled view.");

            popupFromPool.BindModelObject(vm);
            return popupFromPool;
        }

        UIRegistryEntry GetEntry(Type viewType)
        {
            if (!_registry.TryGetByViewType(viewType, out var e) || e == null)
                throw new InvalidOperationException($"{viewType.Name} not found in Registry.");

            return e;
        }

        void Release(UIRegistryEntry entry, PopupViewBase popup)
        {
            if (!popup)
                return;

            var v = popup.GetComponent<UIView>();
            if (!v)
                return;

            var type = popup.GetType();

            // 엔트리가 풀을 안 쓰거나, 해당 타입 풀이 없으면 Destroy
            if (!entry.UsePool || !_pools.TryGetValue(type, out var pool))
            {
                _factory.Destroy(entry, v);
                return;
            }

            pool.Return(v);
        }

        ObjectPool<UIView> GetPool(Type type, UIRegistryEntry entry)
        {
            if (_pools.TryGetValue(type, out var pool))
                return pool;

            int capacity = entry.Capacity;
            int warm     = entry.WarmUp;

            var pFactory = new UIPoolFactory(_factory, entry, _popupRoot);

            pool = new ObjectPool<UIView>(
                pFactory,
                capacity,
                onBeforeReturn: v =>
                {
                    if (!v) return;
                    v.gameObject.SetActive(false);
                    v.transform.SetParent(_popupRoot, false);
                },
                onAfterRent: v =>
                {
                    if (!v) return;
                    v.gameObject.SetActive(true);
                    v.transform.SetParent(_popupRoot, false);
                }
            );

            if (warm > 0)
                pool.WarmupAsync(warm).Forget();

            _pools.Add(type, pool);
            return pool;
        }

        // ======================================================
        // Nested types
        // ======================================================

        readonly struct ActivePopup
        {
            public readonly UIRegistryEntry Entry;
            public readonly PopupViewBase   View;

            public ActivePopup(UIRegistryEntry e, PopupViewBase v)
            {
                Entry = e;
                View  = v;
            }
        }

        abstract class PopupRequestBase
        {
            public abstract UniTask<(UIRegistryEntry entry, PopupViewBase view)>
                CreateAndShowAsync(PopupService svc, CancellationToken ct);
        }

        sealed class PopupRequest<TView> : PopupRequestBase
            where TView : PopupViewBase
        {
            readonly object _vm;
            readonly UniTaskCompletionSource<TView> _tcs;

            public PopupRequest(object vm, UniTaskCompletionSource<TView> tcs)
            {
                _vm  = vm;
                _tcs = tcs;
            }

            public override async UniTask<(UIRegistryEntry entry, PopupViewBase view)>
                CreateAndShowAsync(PopupService svc, CancellationToken ct)
            {
                var popup = await svc.CreateAsync<TView>(_vm, ct);
                popup.Show();

                var entry = svc.GetEntry(typeof(TView));

                _tcs.TrySetResult(popup);
                return (entry, popup);
            }
        }
    }
}
