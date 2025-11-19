// using Core.Engine.Command;
// using Core.Engine.EventBus;
// using Core.Engine.Schedulers;
// using VContainer;
// using VContainer.Unity;
//
//
// namespace Engine.VContainer.Core
// {
//     public sealed class CoreEngineInstaller : IInstaller
//     {
//         public void Install(IContainerBuilder builder)
//         {
//             builder.Register<IEventBus, global::Core.Engine.EventBus.EventBus>(Lifetime.Singleton);
//             builder.Register<EventBusAutoSubscriber>(Lifetime.Singleton)
//                 .As<IStartable>();
//
//             builder.Register<CommandHistory>(Lifetime.Singleton)
//                 .As<ICommandHistory>();
//             builder.Register<CommandBus>(Lifetime.Singleton)
//                 .As<ICommandBus>();
//
//             builder.Register<ITimerScheduler, TimerScheduler>(Lifetime.Singleton);
//         }
//     }
// }