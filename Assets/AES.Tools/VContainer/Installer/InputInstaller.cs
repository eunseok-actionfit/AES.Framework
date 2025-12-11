using AES.Tools.Core;
using AES.Tools.Platform;
using AES.Tools.VContainer.Bootstrap;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer.Installer
{
    public sealed class InputInstaller : IInstaller
    {
        private readonly InputConfig _config;

        public InputInstaller(InputConfig config)
        {
            _config = config;
        }

        public void Install(IContainerBuilder builder)
        {
            builder.RegisterInstance(_config);

#if UNITY_STANDALONE
            builder.Register<IPointerSource, MousePointerSource>(Lifetime.Singleton);
#else
            builder.Register<IPointerSource, TouchPointerSource>(Lifetime.Singleton);
#endif
            builder.Register<IInputService, InputService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InputBootstrap>();
        }
    }
    

}

