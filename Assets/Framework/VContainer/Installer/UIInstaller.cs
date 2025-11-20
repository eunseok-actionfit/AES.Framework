using AES.Tools.Core;
using AES.Tools.Core.Controller;
using AES.Tools.Core.Root;
using AES.Tools.Services.Factory;
using AES.Tools.Services.Guards;
using AES.Tools.Services.Registry;
using VContainer.Bootstrap;
using VContainer.Unity;


namespace VContainer.Installer
{
    public sealed class UIInstaller : IInstaller
    {
        private readonly UIRegistrySO registry;
        public UIInstaller(UIRegistrySO registry) => this.registry = registry;

        public void Install(IContainerBuilder builder)
        {
            builder.Register<UIRootProvider>(Lifetime.Singleton).As<IUIRootProvider>();
            builder.Register<InputGuardService>(Lifetime.Singleton).As<IInputGuard>();
            builder.Register<UiLockService>(Lifetime.Singleton).As<IUiLockService>();

            builder.Register<UIFactory>(Lifetime.Singleton);
            builder.Register<IUIFactory, UIFactory>(Lifetime.Singleton);

            builder.RegisterInstance(registry).As<IUIWindowRegistry>();

            builder.Register<IUIController, UIController>(Lifetime.Singleton);

            builder.RegisterEntryPoint<UIBootstrap>();
        }
    }
}