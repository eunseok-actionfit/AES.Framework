// File: UIManager.cs
using System.Collections.Generic;
using Core.Systems.Pooling;
using Core.Systems.UI.Components.Transitions;
using Core.Systems.UI.Components.Transitions.TransitionAsset.Group;
using Core.Systems.UI.Core.UIRoot;
using Core.Systems.UI.Factory;
using Core.Systems.UI.Registry;
using UnityEngine;

namespace Core.Systems.UI.Core.UIManager
{
    public sealed partial class UIController : IUIController
    {
        private readonly IUIRootProvider _provider;
        private readonly IUIFactory      _factory;
        private readonly IUIWindowRegistry _registry;
        

        // 키 타입을 UIWindowKey로 교체
        private readonly Dictionary<UIWindowKey, UIView.UIView> _open      = new();
        private readonly Dictionary<UIView.UIView, UIWindowKey> _instToKey = new();

        private readonly Dictionary<UIWindowKey, (UIRegistryEntry entry, ObjectPool<UIView.UIView> pool)> _pooled = new();
        private readonly Dictionary<Transform, List<UIView.UIView>> _stackByParent = new();

        private readonly ITransition _winFx = new CanvasGroupFade(0.12f);
        private readonly ITransition _hudFx = new CanvasGroupFade(0.12f);
        private readonly ITransition _popFx = new CanvasGroupFade(0.12f);
        private readonly ITransition _ovlFx = new CanvasGroupFade(0.08f);

        // runtime 정책 상태
        private readonly Dictionary<UIWindowKey, List<UIView.UIView>> _multiInstances = new();
        private readonly Dictionary<UIExclusiveGroup, List<UIView.UIView>> _openByGroup = new();
        private readonly HashSet<UIWindowKey> _animating = new();
        private readonly HashSet<UILayer.UILayer> _subscribedLayers = new();

        public UIController(IUIRootProvider provider, IUIFactory factory, IUIWindowRegistry registry)
        {
            _provider  = provider;
            _factory   = factory;
            _registry  = registry;
            EnsureAllPools();
        }
    }
}