using AES.Tools.Registry;
using UnityEngine;


namespace AES.Tools.Core
{
    public sealed partial class UIController
    {
        private static UIRootRole RoleOf(UIRootRole scope) =>
            scope == UIRootRole.Global ? UIRootRole.Global : UIRootRole.Local;

 
        private Transform ResolveParent(UIRoot root, UIRegistryEntry entry, out UILayer layer)
        {
            var kind = entry?.Kind ?? UILayerKind.Overlay;

            layer = kind switch
            {
                UILayerKind.Window => root.WindowLayer,
                UILayerKind.Hud    => root.HudLayer,
                UILayerKind.Popup  => root.PopupLayer,
                _                  => root.OverlayLayer
            };
            
            bool useSafe = layer.Policy.UseSafeArea;
            return useSafe && layer.Content ? layer.Content : layer.transform;
        }


        private Components.Transitions.ITransition GetFxFor(UIRoot root, Transform parent, UIRegistryEntry _)
        {
            if (parent == root.WindowLayer.transform) return _winFx;
            if (parent == root.HudLayer.transform) return _hudFx;
            if (parent == root.PopupLayer.transform) return _popFx;
            return _ovlFx;
        }

        private Components.Transitions.ITransition CreateLayerBaseFx(Transform parent)
        {
            var layer = parent ? parent.GetComponent<UILayer>() : null;
            var asset = layer?.Policy?.BaseTransitionAsset;
            return asset ? asset : null;
        }

        private Components.Transitions.ITransition PickFx(UIRoot root, Transform parent, UIRegistryEntry entry)
        {
            var layerFx = CreateLayerBaseFx(parent);   // Layer 기본 FX
            return layerFx ?? GetFxFor(root, parent, entry); // 레이어 종류별 기본
        }

        private static UILayer AsLayer(Transform parent) => parent ? parent.GetComponent<UILayer>() : null;

        private void PrepareLayer(UILayer layer)
        {
            if (layer == null) return;
            layer.EnsureCameraBound();
            if (layer.Policy.UseSafeArea) layer.ApplySafeArea();

            if (_subscribedLayers.Add(layer)) {
                var catcher = layer.EnsureClickCatcherReady();
                if (catcher != null)
                    catcher.OnClicked += () => OnLayerOutsideClicked(layer);
            }
        }

        private void OnLayerOutsideClicked(UILayer layer)
        {
            if (layer == null) return;

            // 1) 블로커가 있으면, "블로커의 현재 부모"를 스택 키로 사용
            Transform parent = null;
            var blocker = layer.Policy.InputBlocker ? layer.Policy.InputBlocker.rectTransform : null;
            if (blocker && blocker.parent) parent = blocker.parent;

            // 2) 없으면 기존 기본 규칙(Fallback)
            if (parent == null)
                parent = layer.Content ? layer.Content : layer.transform;

            // 3) 스택 조회 (Content/transform 양쪽 Fallback 포함)
            if (!_stackByParent.TryGetValue(parent, out var list) || list == null || list.Count == 0)
            {
                // Fallback: 반대편 parent도 한 번 더 시도
                var alt = (parent == layer.Content) ? layer.transform
                    : (layer.Content ? (Transform)layer.Content : null);
                if (alt == null || !_stackByParent.TryGetValue(alt, out list) || list == null || list.Count == 0)
                    return;
            }

            var top = list[^1];

            // Entry가 없어도 Hints만으로 평가 가능하도록(동적 인스턴스 방어)
            TryGetEntry(top, out var entry);
            var hints = GetHints(top);

            var effClose = (entry != null)
                ? GetEffectiveCloseOn(hints)
                : (hints != null && hints.closeOn.enabled ? hints.closeOn.value : UICloseOn.None);

            if (effClose == UICloseOn.ClickOutside || effClose == UICloseOn.BackOrOutside)
                _ = HideInstanceAsync(top);
        }
        public void OnBackKey()
        {
            UIView candidate = null;
            foreach (var kv in _stackByParent) {
                var list = kv.Value;
                if (list.Count == 0) continue;

                var top = list[^1];
                if (!TryGetEntry(top, out _)) continue;
                var hints = GetHints(top);
                var effClose = GetEffectiveCloseOn(hints);
                if (effClose is UICloseOn.BackKey or UICloseOn.BackOrOutside) {
                    candidate = top;
                    break;
                }
            }

            if (candidate != null) _ = HideInstanceAsync(candidate);
        }

        private bool TryGetEntry(UIView v, out UIRegistryEntry entry)
        {
            entry = null;
            
            if (_instToKey.TryGetValue(v, out var key) && _registry.TryGet(key, out entry))
                return true;

            key = default;
            foreach (var kv in _open)
            {
                if (kv.Value == v)
                {
                    key = kv.Key;
                    break;
                }
            }
            
            return !Equals(key, default(UIWindowKey)) && _registry.TryGet(key, out entry);
        }

        private UIWindowKey GetKeyOf(UIView v)
        {
            if (_instToKey.TryGetValue(v, out var key))
                return key;

            foreach (var kv in _open)
            {
                if (kv.Value == v)
                    return kv.Key;
            }

            return default;
        }
    }
}