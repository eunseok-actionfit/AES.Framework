using VContainer.Unity;


namespace VContainer.Installer
{
    public sealed class CoreEngineInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            // builder.Register<CommandHistory>(Lifetime.Singleton)
            //     .As<ICommandHistory>();
            // builder.Register<CommandBus>(Lifetime.Singleton)
            //     .As<ICommandBus>();
        }
    }
}