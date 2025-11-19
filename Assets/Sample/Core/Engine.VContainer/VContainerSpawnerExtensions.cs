// using Core.Engine.AssetLoading;
// using Core.Systems.Spawning;
// using UnityEngine;
// using VContainer;
// using VContainer.Unity;
//
//
// namespace Engine.VContainer.Core
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