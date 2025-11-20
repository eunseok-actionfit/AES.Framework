// File: UIManager.cs
using System.Collections.Generic;
using AES.Tools.Core.Layer;
using AES.Tools.Core.Policies;
using AES.Tools.Core.Root;
using AES.Tools.Core.View;
using AES.Tools.Services.Factory;
using AES.Tools.Services.Registry;
using AES.Tools.Transitions.Assets.Group;
using UnityEngine;


namespace AES.Tools.Core.Controller
{
    public sealed partial class UIController : IUIController
    {
        private readonly IUIRootProvider _provider;
        private readonly IUIFactory      _factory;
        private readonly IUIWindowRegistry _registry;
        

        // 키 타입을 UIWindowKey로 교체
        private readonly Dictionary<UIWindowKey, UIView> _open      = new();
        private readonly Dictionary<UIView, UIWindowKey> _instToKey = new();

        private readonly Dictionary<UIWindowKey, (UIRegistryEntry entry, ObjectPool<UIView> pool)> _pooled = new();
        private readonly Dictionary<Transform, List<UIView>> _stackByParent = new();

        private readonly Transitions.ITransition _winFx = new CanvasGroupFade(0.12f);
        private readonly Transitions.ITransition _hudFx = new CanvasGroupFade(0.12f);
        private readonly Transitions.ITransition _popFx = new CanvasGroupFade(0.12f);
        private readonly Transitions.ITransition _ovlFx = new CanvasGroupFade(0.08f);

        // runtime 정책 상태
        private readonly Dictionary<UIWindowKey, List<UIView>> _multiInstances = new();
        private readonly Dictionary<UIExclusiveGroup, List<UIView>> _openByGroup = new();
        private readonly HashSet<UIWindowKey> _animating = new();
        private readonly HashSet<UILayer> _subscribedLayers = new();

        public UIController(IUIRootProvider provider, IUIFactory factory, IUIWindowRegistry registry)
        {
            _provider  = provider;
            _factory   = factory;
            _registry  = registry;
            EnsureAllPools();
        }
    }
}