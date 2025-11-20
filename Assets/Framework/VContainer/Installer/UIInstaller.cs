using AES.Tools.Core;
using AES.Tools.Factory;
using AES.Tools.Guards;
using AES.Tools.Registry;
using VContainer.Unity;


namespace VContainer.Installer
{
    public sealed class UIInstaller : IInstaller
    {
        private readonly UIWindowRegistrySO registry;
        public UIInstaller(UIWindowRegistrySO registry) => this.registry = registry;

        public void Install(IContainerBuilder builder)
        {
            builder.Register<UIRootProvider>(Lifetime.Singleton).As<IUIRootProvider>();
            builder.Register<InputGuardService>(Lifetime.Singleton).As<IInputGuard>();
            builder.Register<UiLockService>(Lifetime.Singleton).As<IUiLockService>();

            builder.Register<UIFactory>(Lifetime.Singleton);
            builder.Register<IUIFactory, UIFactory>(Lifetime.Singleton);

            builder.RegisterInstance(registry).As<IUIWindowRegistry>();

            builder.Register<IUIController, UIController>(Lifetime.Singleton);
            
            builder.Register<UIBootstrap>(Lifetime.Singleton).As<IStartable>();
        }
    }
}