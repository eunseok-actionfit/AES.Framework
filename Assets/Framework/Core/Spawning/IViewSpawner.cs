using UnityEngine;


namespace AES.Tools
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