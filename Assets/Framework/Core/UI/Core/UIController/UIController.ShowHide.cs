using System;
using System.Collections.Generic;
using System.Threading;
using Core.Systems.UI.Core.UILayer;
using Core.Systems.UI.Core.UIRoot;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace Core.Systems.UI.Core.UIManager
{
    public sealed partial class UIController
    {
        public async UniTask<UIView.UIView> ShowAsync<TEnum>(TEnum id, object model = null, CancellationToken ct = default)
            where TEnum : Enum
        {
            var key = UIWindowKey.FromEnum(id);

            if (!_registry.TryGet(key, out var entry))
                throw new InvalidOperationException($"UI id not registered: {typeof(TEnum).Name}.{id}");

            // Concurrency (애니 중 재호출)
            if (_animating.Contains(key))
            {
                var policy = entry.Concurrency;

                if (policy == UIConcurrency.Ignore)
                    return GetOpen<UIView.UIView, TEnum>(id);

                if (policy == UIConcurrency.UpdateModel && _open.TryGetValue(key, out var v))
                {
                    // 필요하면 모델 업데이트 로직 추가
                    // await v.ShowAsync(model, default, ct);
                    return v;
                }

                if (policy == UIConcurrency.Replace && _open.TryGetValue(key, out var ov))
                    await HideInstanceCore(ov, key, ct);
            }

            // 이미 열린 인스턴스가 있는 경우 InstancePolicy 적용
            if (_open.TryGetValue(key, out var current))
            {
                var ip = entry.InstancePolicy;

                if (ip is UIInstancePolicy.Singleton or UIInstancePolicy.DenyIfOpen or UIInstancePolicy.ReplaceExisting)
                {
                    if (ip == UIInstancePolicy.ReplaceExisting)
                        await HideAsync(id, ct);
                    //else if (ip == UIInstancePolicy.DenyIfOpen) { }

                    return current;
                }
            }

            // 모델 타입 검증
            if (entry.DataContractType != null && model != null &&
                !entry.DataContractType.IsInstanceOfType(model))
                throw new InvalidOperationException(
                    $"UI '{typeof(TEnum).Name}.{id}' expects model type '{entry.DataContractType.Name}', got '{model.GetType().Name}'");

            // Root/Layer/Parent 준비
            var role = RoleOf(entry.Scope);
            var root = _provider.Get(role) ?? throw new InvalidOperationException($"UIRoot({role}) not found.");
            var parent = ResolveParent(root, entry, out var layer);
            PrepareLayer(layer);

            // ExclusiveGroup 정리
            if (entry.ExclusiveGroup != UIExclusiveGroup.None &&
                _openByGroup.TryGetValue(entry.ExclusiveGroup, out var glist))
            {
                foreach (var v in glist.ToArray())
                {
                    if (entry.InstancePolicy == UIInstancePolicy.DenyIfOpen)
                        return v;

                    var vid = GetKeyOf(v);
                    await HideInstanceCore(v, vid, ct);
                }
            }

            // 인스턴스 확보
            UIView.UIView inst;

            if (entry.UsePool)
            {
                if (!_pooled.TryGetValue(key, out var pooled))
                    throw new InvalidOperationException($"Pooling configured but pool missing for '{typeof(TEnum).Name}.{id}'");

                inst = await pooled.pool.Rent(ct);
                if (inst.transform.parent != parent) inst.transform.SetParent(parent, false);
            }
            else
            {
                inst = await _factory.CreateAsync(entry, parent, ct);
            }

            _open[key] = inst;

            // SafeArea / Parent 교정
            var hints = GetHints(inst);
            var expectedParent = (layer.Content != null) ? layer.Content : layer.transform;
            if (inst.transform.parent != expectedParent)
                inst.transform.SetParent(expectedParent, false);

            var effClose = GetEffectiveCloseOn(hints);
            if ((effClose == UICloseOn.ClickOutside || effClose == UICloseOn.BackOrOutside) &&
                layer != null && !layer.Policy.BlocksInput)
            {
                Debug.LogWarning($"[UIManager] '{typeof(TEnum).Name}.{id}' uses CloseOn={effClose} but the layer '{layer.name}' has BlocksInput=false.");
            }

            ApplyViewSafeAreaOverrides(inst, hints);

            if (layer != null && layer.Policy.BlocksInput)
            {
                inst.transform.SetAsLastSibling();
                layer.SetInputBlocker(true);
                layer.PlaceBlockerBelowTop(inst.transform);
            }

            _animating.Add(key);

            try
            {
                var fx = PickFx(root, expectedParent, entry);
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, inst.DestroyToken))
                {
                    var linked = cts.Token;
                    await inst.ShowAsync(model, fx, linked);
                }

                PushStack(expectedParent, inst);

                var asLayer = layer;
                if (asLayer != null && asLayer.Policy.BlocksInput)
                {
                    if (_stackByParent.TryGetValue(expectedParent, out var l) && l.Count > 0)
                        asLayer.PlaceBlockerBelowTop(l[^1].transform);
                }

                if (entry.ExclusiveGroup != UIExclusiveGroup.None)
                {
                    if (!_openByGroup.TryGetValue(entry.ExclusiveGroup, out var list))
                        _openByGroup[entry.ExclusiveGroup] = list = new List<UIView.UIView>(4);

                    list.Add(inst);
                }
            }
            finally
            {
                _animating.Remove(key);
            }

            return inst;
        }

        public async UniTask HideAsync<TEnum>(TEnum id, CancellationToken ct = default)
            where TEnum : Enum
        {
            var key = UIWindowKey.FromEnum(id);
            if (!_open.TryGetValue(key, out var view))
                return;

            await HideInstanceCore(view, key, ct);
        }

        public async UniTask<UIView.UIView> ShowInstanceAsync<TEnum>(TEnum id, object model = null, CancellationToken ct = default)
            where TEnum : Enum
        {
            var key = UIWindowKey.FromEnum(id);

            if (!_registry.TryGet(key, out var entry))
                throw new InvalidOperationException($"UI id not registered: {typeof(TEnum).Name}.{id}");

            if (entry.InstancePolicy is UIInstancePolicy.Singleton or UIInstancePolicy.DenyIfOpen)
                return await ShowAsync(id, model, ct);

            var role = RoleOf(entry.Scope);
            var root = _provider.Get(role) ?? throw new InvalidOperationException($"UIRoot({role}) not found.");
            var parent = ResolveParent(root, entry, out var layer);
            PrepareLayer(layer);

            if (entry.ExclusiveGroup != UIExclusiveGroup.None &&
                _openByGroup.TryGetValue(entry.ExclusiveGroup, out var glist))
            {
                foreach (var v in glist.ToArray())
                {
                    var vid = GetKeyOf(v);
                    await HideInstanceCore(v, vid, ct);
                }
            }

            UIView.UIView inst;

            if (entry.UsePool)
            {
                if (!_pooled.TryGetValue(key, out var pooled))
                    throw new InvalidOperationException($"Pooling configured but pool missing for '{typeof(TEnum).Name}.{id}'");

                inst = await pooled.pool.Rent(ct);
                if (inst.transform.parent != parent) inst.transform.SetParent(parent, false);
            }
            else
            {
                inst = await _factory.CreateAsync(entry, parent, ct);
            }

            _instToKey[inst] = key;

            var hints = GetHints(inst);
            var expectedParent = (layer.Content != null) ? layer.Content : layer.transform;
            if (inst.transform.parent != expectedParent)
                inst.transform.SetParent(expectedParent, false);

            ApplyViewSafeAreaOverrides(inst, hints);

            if (layer != null && layer.Policy.BlocksInput)
            {
                inst.transform.SetAsLastSibling();
                layer.SetInputBlocker(true);
                layer.PlaceBlockerBelowTop(inst.transform);
            }

            var fx = PickFx(root, expectedParent, entry);
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, inst.DestroyToken))
            {
                var linked = cts.Token;
                await inst.ShowAsync(model, fx, linked);
            }

            PushStack(expectedParent, inst);

            var asLayer = layer;
            if (asLayer != null && asLayer.Policy.BlocksInput)
            {
                if (_stackByParent.TryGetValue(expectedParent, out var l) && l.Count > 0)
                    asLayer.PlaceBlockerBelowTop(l[^1].transform);
            }

            if (entry.ExclusiveGroup != UIExclusiveGroup.None)
            {
                if (!_openByGroup.TryGetValue(entry.ExclusiveGroup, out var list))
                    _openByGroup[entry.ExclusiveGroup] = list = new List<UIView.UIView>(4);

                list.Add(inst);
            }

            return inst;
        }

        public async UniTask HideInstanceAsync(UIView.UIView view, CancellationToken ct = default)
        {
            if (view == null) return;

            var key = GetKeyOf(view);
            await HideInstanceCore(view, key, ct);
        }

        private async UniTask HideInstanceCore(UIView.UIView view, UIWindowKey key, CancellationToken ct)
        {
            if (view == null) return;
            if (!_registry.TryGet(key, out var entry))
            {
                // 등록이 사라진 뷰는 그냥 Destroy
                UnityEngine.Object.Destroy(view.gameObject);
                return;
            }

            var role = RoleOf(entry.Scope);
            var root = _provider.Get(role) ?? _provider.Get(UIRoot.UIRootRole.Local) ?? _provider.Get(UIRoot.UIRootRole.Global);
            var parent = (root != null) ? ResolveParent(root, entry, out _) : null;
            var fx = (root != null) ? PickFx(root, parent, entry) : null;

            _animating.Add(key);

            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, view.DestroyToken))
                {
                    var linked = cts.Token;
                    await view.HideAsync(fx, linked);
                }

                if (parent != null)
                    PopStack(parent, view);

                var lay = AsLayer(parent);
                if (lay != null && lay.Policy.BlocksInput)
                {
                    if (parent != null && _stackByParent.TryGetValue(parent, out var l) && l.Count > 0)
                        lay.PlaceBlockerBelowTop(l[^1].transform);
                    else
                        lay.SetInputBlocker(false);
                }

                if (entry.ExclusiveGroup != UIExclusiveGroup.None &&
                    _openByGroup.TryGetValue(entry.ExclusiveGroup, out var glist))
                    glist.Remove(view);

                RestoreViewRect(view);

                if (_pooled.TryGetValue(key, out var pooled))
                {
                    view.gameObject.SetActive(false);
                    if (entry is { ReturnDelay: > 0f })
                        await UniTask.Delay(TimeSpan.FromSeconds(entry.ReturnDelay), cancellationToken: ct);

                    pooled.pool.Return(view);
                }
                else
                {
                    _factory.Destroy(entry, view);
                }

                _open.Remove(key);
                _instToKey.Remove(view);
            }
            finally
            {
                _animating.Remove(key);
            }
        }
        

        public async UniTask CloseAllAsync(UIRootRole role, CancellationToken ct = default)
        {
            var root = _provider.Get(role);
            if (root == null) 
                return;

            var toClose = new List<(UIView.UIView view, UIWindowKey key)>();

            void Collect(UILayer.UILayer layer)
            {
                if (layer == null) 
                    return;

                var parent = (layer.Content != null) ? layer.Content : layer.transform;

                if (_stackByParent.TryGetValue(parent, out var list))
                {
                    foreach (var v in list)
                    {
                        if (v == null) 
                            continue;

                        var key = GetKeyOf(v);
                        toClose.Add((v, key));
                    }
                }
            }

            Collect(root.WindowLayer);
            Collect(root.HudLayer);
            Collect(root.PopupLayer);
            Collect(root.OverlayLayer);

            foreach (var (view, key) in toClose)
            {
                if (view == null)
                    continue;

                await HideInstanceCore(view, key, ct);
            }
        }
    }
}
