#if AESFW_UNITASK
using Cysharp.Threading.Tasks;
using Awaitable = Cysharp.Threading.Tasks.UniTask;
#elif UNITY_2023_1_OR_NEWER
using Awaitable = UnityEngine.Awaitable;
#else
using Awaitable = System.Threading.Tasks.Task;
#endif

using UnityEngine;

namespace Core.Systems.Spawning
{
    public interface IViewSpawner<T> where T : Component
    {
        Awaitable<T> Spawn();
        Awaitable<T> Spawn(Transform parent);
        Awaitable<T> Spawn(Vector3 position, Quaternion rotation);
        Awaitable<T> Spawn(Vector3 position, Quaternion rotation, Transform parent);
        void Despawn(T view);
    }
}