// using Core.Systems.UI.Components.Buttons.Feedback;
// using Core.Systems.UI.Core.UIManager;
// using Core.Systems.UI.Core.UIRoot;
// using Core.Systems.UI.Factory;
// using Core.Systems.UI.Guards;
// using Core.Systems.UI.Registry;
// using VContainer;
// using VContainer.Unity;
//
//
// namespace Engine.VContainer.Core
// {
//     public sealed class UIInstaller : IInstaller
//     {
//         private readonly UIWindowRegistrySO registry;
//         public UIInstaller(UIWindowRegistrySO registry) { this.registry = registry; }
//
//         public void Install(IContainerBuilder builder)
//         {
//             builder.Register<UIRootProvider>(Lifetime.Singleton).As<IUIRootProvider>();
//
//             builder.Register<InputGuardService>(Lifetime.Singleton).As<IInputGuard>();
//             builder.Register<UiLockService>(Lifetime.Singleton).As<IUiLockService>();
//             builder.Register<DotweenButtonFeedback>(Lifetime.Singleton).As<IButtonFeedback>();
//
//             builder.Register<UIFactory>(Lifetime.Singleton);
//             builder.Register<IUIFactory, UIFactory>(Lifetime.Singleton);
//
//             builder.RegisterInstance(registry).As<IUIWindowRegistry>();
//
//             builder.Register<UIService>(Lifetime.Singleton);
//             builder.Register<IUIService, UIService>(Lifetime.Singleton);
//         }
//     }
// }