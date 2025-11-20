using System;
using AES.Tools.Guards;

namespace AES.Tools.Core
{
    public static class UiServices
    {
        private static IInputGuard _inputGuard;
        public static IInputGuard InputGuard
        {
            get => _inputGuard ??= new InputGuardService();
            internal set => _inputGuard = value;
        }

        private static IUiLockService _uiLock;
        public static IUiLockService UiLock
        {
            get => _uiLock ??= new UiLockService();
            internal set => _uiLock = value;
        }

        private static IUIRootProvider _root;
        public static IUIRootProvider UIRootProvider
        {
            get => _root ??= new UIRootProvider();
            internal set => _root = value;
        }

        private static IUIController _controller;
        public static IUIController UIController
        {
            get => _controller 
                   ?? throw new InvalidOperationException("UIController not initialized.");
            internal set => _controller = value;
        }
    }
}