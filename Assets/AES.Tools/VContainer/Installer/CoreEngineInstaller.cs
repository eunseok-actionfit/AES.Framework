using AES.Tools.TBC.CommandSystem;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer.Installer
{
    public sealed class CoreEngineInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<CommandHistory>(Lifetime.Singleton)
                .As<ICommandHistory>();
            builder.Register<CommandBus>(Lifetime.Singleton)
                .As<ICommandBus>();
        }
    }
}