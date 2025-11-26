// using Cysharp.Threading.Tasks;
// using UnityEngine;
//
//
// namespace AES.Tools
// {
//     public interface IViewSpawner<T> where T : Component
//     {
//         UniTask<T> Spawn();
//         UniTask<T> Spawn(Transform parent);
//         UniTask<T> Spawn(Vector3 position, Quaternion rotation);
//         UniTask<T> Spawn(Vector3 position, Quaternion rotation, Transform parent);
//         void Despawn(T view);
//     }
// }