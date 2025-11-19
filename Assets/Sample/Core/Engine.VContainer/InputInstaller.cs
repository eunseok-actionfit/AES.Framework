// using Core.Systems.Input;
// using Core.Systems.Input.Core;
// using Core.Systems.Input.Platform;
// using VContainer;
// using VContainer.Unity;
//
//
// namespace Engine.VContainer.Core
// {
//     public sealed class InputInstaller : IInstaller
//     {
//         private readonly InputConfig _config;
//
//         public InputInstaller(InputConfig config)
//         {
//             _config = config;
//         }
//
//         public void Install(IContainerBuilder builder)
//         {
//             builder.RegisterInstance(_config);
//
// #if UNITY_STANDALONE
//             builder.Register<IPointerSource, MousePointerSource>(Lifetime.Singleton);
// #else
//             builder.Register<IPointerSource, TouchPointerSource>(Lifetime.Singleton);
// #endif
//             builder.Register<IInputService, InputService>(Lifetime.Singleton);
//             builder.RegisterEntryPoint<InputEntryPoint>();
//         }
//     }
//     
//     public sealed class InputEntryPoint : ITickable
//     {
//         private readonly IInputService _input;
//         public InputEntryPoint(IInputService input) => _input = input;
//         public void Tick() => _input.Tick(UnityEngine.Time.unscaledDeltaTime);
//     }
// }
//
