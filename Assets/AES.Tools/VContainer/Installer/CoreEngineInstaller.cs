using AES.Tools.TimeManager.Schedulers;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer.Installer
{
    public sealed class CoreEngineInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // TimerScheduler 등록
            builder.Register<TimerScheduler>(Lifetime.Singleton)
                .As<ITimerScheduler>();
            
            builder.Register<CommandHistory>(Lifetime.Singleton)
                .As<ICommandHistory>();
            builder.Register<CommandBus>(Lifetime.Singleton)
                .As<ICommandBus>();
        }
    }
}