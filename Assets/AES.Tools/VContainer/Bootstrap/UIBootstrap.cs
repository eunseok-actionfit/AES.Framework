// using AES.Tools.Controller;
// using AES.Tools.Root;
// using VContainer.Unity;
//
//
// namespace AES.Tools.VContainer
// {
//     public class UIBootstrap : IStartable
//     {
//         private readonly IUIRootProvider _root;
//         private readonly IUIController _controller;
//         private readonly IInputGuard _guard;
//         private readonly IUiLockService _lock;
//
//         public UIBootstrap(
//             IUIRootProvider root,
//             IUIController controller,
//             IInputGuard guard,
//             IUiLockService uiLock)
//         {
//             _root = root;
//             _controller = controller;
//             _guard = guard;
//             _lock = uiLock;
//         }
//
//         public void Start()
//         {
//            // UiServiceLocator.UIRootProvider = _root;
//             UiServiceLocator.UIController = _controller;
//             UiServiceLocator.InputGuard = _guard;
//             UiServiceLocator.UiLock = _lock;
//         }
//     }
// }