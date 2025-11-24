// using System.Collections.Generic;
// using AES.Tools.Assets.Group;
// using AES.Tools.Layer;
// using AES.Tools.Policies;
// using AES.Tools.Root;
// using AES.Tools.View;
// using UnityEngine;
//
//
// namespace AES.Tools.Controller
// {
//     public sealed partial class UIController : IUIController
//     {
//         private readonly IUIRootProvider _provider;
//         private readonly IUIFactory      _factory;
//         private readonly IURegistry _registry;
//         
//
//         // 키 타입을 UIWindowKey로 교체
//         private readonly Dictionary<UIWindowKey, UIView> _open      = new();
//         private readonly Dictionary<UIView, UIWindowKey> _instToKey = new();
//
//         private readonly Dictionary<UIWindowKey, (UIRegistryEntry entry, ObjectPool<UIView> pool)> _pooled = new();
//         private readonly Dictionary<Transform, List<UIView>> _stackByParent = new();
//
//         private readonly IUITransition _winFx = new CanvasGroupFade(0.12f);
//         private readonly IUITransition _hudFx = new CanvasGroupFade(0.12f);
//         private readonly IUITransition _popFx = new CanvasGroupFade(0.12f);
//         private readonly IUITransition _ovlFx = new CanvasGroupFade(0.08f);
//
//         // runtime 정책 상태
//         private readonly Dictionary<UIWindowKey, List<UIView>> _multiInstances = new();
//         private readonly Dictionary<UIExclusiveGroup, List<UIView>> _openByGroup = new();
//         private readonly HashSet<UIWindowKey> _animating = new();
//         private readonly HashSet<UILayer> _subscribedLayers = new();
//
//         public UIController(IUIFactory factory, IURegistry registry)
//         {
//             _provider  = UiServiceLocator.UIRootProvider;
//             _factory   = factory;
//             _registry  = registry;
//             EnsureAllPools();
//         }
//     }
// }