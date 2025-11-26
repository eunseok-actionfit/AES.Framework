// using System;
// using AES.Tools.Controller;
// using AES.Tools.Root;
// using UnityEngine;
// using VContainer;
// using VContainer.Unity;
//
//
// namespace AES.Tools.VContainer.Installer
// {
//     public sealed class UIInstaller : IInstaller
//     {
//         private readonly UIRegistrySO registry;
//         public UIInstaller(UIRegistrySO registry) => this.registry = registry;
//
//         public void Install(IContainerBuilder builder)
//         {
//             builder.Register<UIRootProvider>(Lifetime.Singleton).As<IUIRootProvider>();
//             builder.Register<InputGuardService>(Lifetime.Singleton).WithParameter<Func<float>>(() => Time.unscaledTime).As<IInputGuard>();
//             builder.Register<UiLockService>(Lifetime.Singleton).As<IUiLockService>();
//
//             builder.Register<UIFactory>(Lifetime.Singleton);
//             builder.Register<IUIFactory, UIFactory>(Lifetime.Singleton);
//
//             builder.RegisterInstance(registry).As<IURegistry>();
//
//          //   builder.Register<IUIController, UIController>(Lifetime.Singleton);
//
//             builder.RegisterEntryPoint<UIBootstrap>();
//         }
//     }
// }