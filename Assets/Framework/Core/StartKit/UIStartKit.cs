using AES.Tools.Controller;
using AES.Tools.Root;


namespace AES.Tools.StartKit
{
    public static class UIStartKit
    {
        public static void Initialize(UIRegistrySO registry)
        {
            var root = new UIRootProvider();
            var factory = new UIFactory();
            var controller = new UIController(factory, registry);

            UiServiceLocator.UIRootProvider = root;
            UiServiceLocator.InputGuard = new InputGuardService();
            UiServiceLocator.UiLock = new UiLockService();
            UiServiceLocator.UIController = controller;
        }
    }
}