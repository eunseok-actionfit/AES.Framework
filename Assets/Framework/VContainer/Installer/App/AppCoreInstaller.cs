using VContainer.AppLifetime;
using VContainer.Unity;


namespace VContainer.Installer.App
{
    public sealed class AppCoreInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ApplicationLifetime>(Lifetime.Singleton)
                .AsSelf()
                .As<IApplicationLifetime>();
        }
    }
}