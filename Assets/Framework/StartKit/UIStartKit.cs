using AES.Tools.Core;
using AES.Tools.Factory;
using AES.Tools.Guards;
using AES.Tools.Registry;

public static class UIStartKit
{
    public static void Initialize(UIWindowRegistrySO registry)
    {
        var root = new UIRootProvider();
        var factory = new UIFactory();
        var controller = new UIController(root, factory, registry);

        UiServices.UIRootProvider = root;
        UiServices.InputGuard = new InputGuardService();
        UiServices.UiLock = new UiLockService();
        UiServices.UIController = controller;
    }
}