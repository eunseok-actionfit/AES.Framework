using AES.Tools.Core;
using AES.Tools.Platform;
using VContainer.Unity;


namespace VContainer.Installer
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
    
    public sealed class InputBootstrap : ITickable
    {
        private readonly IInputService _input;
        public InputBootstrap(IInputService input) => _input = input;
        public void Tick() => _input.Tick(UnityEngine.Time.unscaledDeltaTime);
    }
}

