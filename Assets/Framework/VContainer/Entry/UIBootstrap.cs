using AES.Tools.Core;
using AES.Tools.Guards;
using VContainer.Unity;

public class UIBootstrap : IStartable
{
    private readonly IUIRootProvider _root;
    private readonly IUIController _controller;
    private readonly IInputGuard _guard;
    private readonly IUiLockService _lock;

    public UIBootstrap(
        IUIRootProvider root,
        IUIController controller,
        IInputGuard guard,
        IUiLockService uiLock)
    {
        _root = root;
        _controller = controller;
        _guard = guard;
        _lock = uiLock;
    }

    public void Start()
    {
        UiServices.UIRootProvider = _root;
        UiServices.UIController = _controller;
        UiServices.InputGuard = _guard;
        UiServices.UiLock = _lock;
    }
}