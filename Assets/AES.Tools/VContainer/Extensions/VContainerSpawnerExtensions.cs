//
//
//
// using Core.Systems.Spawning;
// using VContainer;
//
//
// namespace AES.Tools.VContainer
// {
//     public static class VContainerSpawnerExtensions
//     {
//         public static void RegisterSpawner<T>(
//             this IContainerBuilder b,
//             SpawnerOptions opt,
//             Lifetime life = Lifetime.Singleton)
//             where T : Component
//         {
//             b.RegisterEntryPoint(r =>
//                     new ViewSpawner<T>(r.Resolve<IAssetInstantiator>(), opt), life)
//                 .As<IViewSpawner<T>>()
//                 .AsSelf();
//         }
//     }
// }