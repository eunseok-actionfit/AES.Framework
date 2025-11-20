using AES.Tools.Core.UIController;
using AES.Tools.Core.UIRoot;
using AES.Tools.Guards;


namespace AES.Tools.Core
{
    public static class UiServices
    {
        public static IInputGuard InputGuard { get; set; } = new InputGuardService();

        public static IUiLockService UiLock { get; set; } = new UiLockService();

        public static IUIRootProvider UIRootProvider { get; set; } = new UIRootProvider();
        
        public static IUIController UIController { get; set; } 

    }
}