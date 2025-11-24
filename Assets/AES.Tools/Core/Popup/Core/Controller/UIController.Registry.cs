using System;
using AES.Tools.Root;
using AES.Tools.View;


namespace AES.Tools.Controller
{
    public sealed partial class UIController
    {
        // UIEntry 개별 풀 생성 (외부에서는 enum 사용)
        public void EnsurePool<TEnum>(TEnum id, int capacity = 8, int warmUp = 0)
            where TEnum : Enum
        {
            var key = UIWindowKey.FromEnum(id);
            if (_pooled.ContainsKey(key))
                return;

            if (!_registry.TryGet(key, out var entry))
                throw new InvalidOperationException($"UI id not registered: {typeof(TEnum).Name}.{id}");

            EnsurePoolCore(key, entry, capacity, warmUp);
        }

        // 등록된 모든 UI 중 Pooling 정책 있는 항목의 풀 생성
        public void EnsureAllPools(UIRootRole fallbackRole = UIRootRole.Global)
        {
            foreach (var kv in _registry.GetAll())
            {
                var key   = kv.Key;
                var entry = kv.Value;

                if (!entry.UsePool)
                    continue;
                if (_pooled.ContainsKey(key))
                    continue;

                EnsurePoolCore(key, entry, entry.Capacity, entry.WarmUp);
            }
        }

        private void EnsurePoolCore(UIWindowKey key, UIRegistryEntry entry, int capacity, int warmUp)
        {
            if (_pooled.ContainsKey(key))
                return;

            var role   = RoleOf(entry.Scope);
            var root   = _provider.Get(role) ?? _provider.Get(UIRootRole.Global);
             ResolveParent(root, entry, out var layer);
             var parent = layer.Content;
            PrepareLayer(layer);
            
            var factory = new UIPoolFactory(_factory, entry, parent);
            var pool    = new ObjectPool<UIView>(factory, capacity, ui => ui.gameObject.SetActive(false) );
            pool.WarmupAsync(warmUp).ForgetWithLog("[UIManager] EnsurePool:");
            _pooled[key] = (entry, pool);
        }

        public void WithPolicy<TEnum>(TEnum id, Action<UIRegistryEntry> edit)
            where TEnum : Enum
        {
            if (edit == null)
                return;

            var key = UIWindowKey.FromEnum(id);
            if (_registry.TryGet(key, out var e))
                edit(e);
        }

        public T GetOpen<T, TEnum>(TEnum id)
            where T : UIView
            where TEnum : Enum
        {
            var key = UIWindowKey.FromEnum(id);
            return _open.TryGetValue(key, out var view) ? view as T : null;
        }

        public bool IsOpen<TEnum>(TEnum id)
            where TEnum : Enum
        {
            var key = UIWindowKey.FromEnum(id);
            return _open.ContainsKey(key);
        }
    }
}
