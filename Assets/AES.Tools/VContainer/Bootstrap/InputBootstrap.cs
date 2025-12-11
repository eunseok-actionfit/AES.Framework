using AES.Tools.Core;
using VContainer.Unity;


namespace AES.Tools.VContainer.Bootstrap
{
    public class InputBootstrap : IStartable, ITickable
    {

        private readonly IInputService _input;
        public InputBootstrap(IInputService input) => _input = input;

        public void Start() => InputServiceLocator.Service = _input;
        public void Tick() => _input.Tick(UnityEngine.Time.unscaledDeltaTime);

    }
}