using UnityEngine;


namespace AES.Tools
{
    public interface IFactory<T>
    {
        T Create();
        void Destroy(T item);
    }

    public sealed class PrefabFactory<T> : IFactory<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;

        public PrefabFactory(T prefab, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
        }

        public T Create()
        {
            var inst = Object.Instantiate(_prefab, _parent);
            return inst;
        }

        public void Destroy(T item)
        {
            if (!item) return;
            Object.Destroy(item.gameObject);
        }
    }
}