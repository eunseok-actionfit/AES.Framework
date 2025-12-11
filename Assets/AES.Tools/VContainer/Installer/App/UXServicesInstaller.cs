using AES.Tools.VContainer.Services.Loading;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer.Installer.App
{
    public sealed class UXServicesInstaller : IInstaller
    {
        public void Install(IContainerBuilder b)
        {
            // b.Register<IToastService, ToastService>(Lifetime.Singleton);
            // b.Register<IGlobalSpinner, GlobalSpinnerService>(Lifetime.Singleton);
             b.Register<LoadingBus>(Lifetime.Singleton)
                 .As<ILoadingBus>();
            
            b.RegisterBuildCallback(resolver =>
            {
                LoadingBusFacade.Instance = resolver.Resolve<ILoadingBus>();
            });
            
             b.Register<ILoadingService, LoadingService>(Lifetime.Singleton);
        }
    }
}