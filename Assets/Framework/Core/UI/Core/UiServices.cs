using Core.Systems.UI.Core.UIManager;
using Core.Systems.UI.Core.UIRoot;
using Core.Systems.UI.Guards;


namespace Core.Systems.UI
{
    public static class UiServices
    {
        public static IInputGuard InputGuard { get; set; } = new InputGuardService();

        public static IUiLockService UiLock { get; set; } = new UiLockService();

        public static IUIRootProvider UIRootProvider { get; set; } = new UIRootProvider();
        
        public static IUIController UIController { get; set; } 

    }
}