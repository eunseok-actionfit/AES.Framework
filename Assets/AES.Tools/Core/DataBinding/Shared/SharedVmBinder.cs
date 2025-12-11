// SharedVmBinder.cs
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AES.Tools
{
    [DisallowMultipleComponent]
    public sealed class SharedVmBinder : MonoBehaviour
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Shared<T> 필드를 가지고 있는 컴포넌트")]
            public MonoBehaviour owner;

            [Tooltip("Shared<T> 필드 이름 (예: \"pivot\")")]
            public string sharedFieldName;

            [Tooltip("해당 Shared<T>와 연결할 VM 경로/방향 정보")]
            public SharedVmBindingInfo binding;
        }

        [SerializeField] private ContextLookupMode lookupMode = ContextLookupMode.Nearest;
        [SerializeField] private string contextName;

        [SerializeField] private List<Entry> entries = new();

        private IBindingContext _ctx;
        private readonly List<(string path, Action<object> cb, object token)> _subscriptions = new();

        public ContextLookupMode LookupMode
        {
            get => lookupMode;
            set => lookupMode = value;
        }

        public string ContextName
        {
            get => contextName;
            set => contextName = value;
        }

        public List<Entry> Entries => entries;

        private void Awake()
        {
            var provider = ResolveProvider();
            if (provider == null)
            {
                Debug.LogWarning($"{name}: IBindingContextProvider 를 찾지 못했습니다. SharedVmBinder 비활성화.", this);
                enabled = false;
                return;
            }

            _ctx = provider.RuntimeContext;
            if (_ctx == null)
            {
                Debug.LogWarning($"{name}: BindingContext 가 null 입니다. SharedVmBinder 비활성화.", this);
                enabled = false;
                return;
            }

            SetupBindings(_ctx);
        }

        private void OnDestroy()
        {
            if (_ctx != null)
            {
                foreach (var (p, cb, token) in _subscriptions)
                    _ctx.RemoveListener(p, cb, token);
            }

            _subscriptions.Clear();
        }

        // 에디터에서 owner만 설정해두고 sharedFieldName 비워놔도
        // OnValidate에서 자동으로 Shared<T> 필드 이름 채워 줄 수 있음.
        private void OnValidate()
        {
#if UNITY_EDITOR
            foreach (var e in entries)
            {
                if (e == null || e.owner == null)
                    continue;

                if (!string.IsNullOrEmpty(e.sharedFieldName))
                    continue;

                var t = e.owner.GetType();
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    if (f.FieldType.IsGenericType &&
                        f.FieldType.GetGenericTypeDefinition() == typeof(Shared<>))
                    {
                        e.sharedFieldName = f.Name;
                        break;
                    }
                }
            }
#endif
        }

        // ────────────────────────────────────────
        // BindingContextProvider 찾기 (ContextBindingBaseEditor와 동일한 규칙)
       // :contentReference[oaicite:2]{index=2}
        // ────────────────────────────────────────
        private IBindingContextProvider ResolveProvider()
        {
            var mode = lookupMode;
            var nameForLookup = contextName;

            switch (mode)
            {
                case ContextLookupMode.Nearest:
                    return GetComponentInParent<IBindingContextProvider>();

                case ContextLookupMode.ByNameInParents:
                    return FindProviderInParentsByName(nameForLookup);

                case ContextLookupMode.ByNameInScene:
                    return FindProviderInSceneByName(nameForLookup);

                default:
                    return GetComponentInParent<IBindingContextProvider>();
            }
        }

        private IBindingContextProvider FindProviderInParentsByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var all = GetComponentsInParent<MonoBehaviour>(true);
            foreach (var mb in all)
            {
                if (mb is IBindingContextProvider p)
                {
                    if (mb is MonoContext dc && dc.ContextName == name)
                        return p;

                    if (mb.gameObject.name == name)
                        return p;
                }
            }

            return null;
        }

        private IBindingContextProvider FindProviderInSceneByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

#if UNITY_2022_2_OR_NEWER
            var all = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
            var all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
            foreach (var mb in all)
            {
                if (mb is IBindingContextProvider p)
                {
                    if (mb is MonoContext dc && dc.ContextName == name)
                        return p;

                    if (mb.gameObject.name == name)
                        return p;
                }
            }

            return null;
        }

        // ────────────────────────────────────────
        // Shared<T> ↔ VM 바인딩
        // 기존 SharedVmBinder.SetupBindings 내용을 그대로 사용 :contentReference[oaicite:3]{index=3}
        // ────────────────────────────────────────
        private void SetupBindings(IBindingContext ctx)
        {
            _subscriptions.Clear();

            foreach (var e in entries)
            {
                if (e == null || e.owner == null || e.binding == null)
                    continue;

                if (string.IsNullOrEmpty(e.binding.vmPath))
                    continue;

                var sharedField = e.owner.GetType().GetField(
                    e.sharedFieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (sharedField == null)
                {
                    Debug.LogWarning($"[{e.owner.GetType().Name}]에 필드 '{e.sharedFieldName}'를 찾을 수 없습니다.", e.owner);
                    continue;
                }

                if (!sharedField.FieldType.IsGenericType ||
                    sharedField.FieldType.GetGenericTypeDefinition() != typeof(Shared<>))
                {
                    Debug.LogWarning($"필드 '{e.sharedFieldName}'는 Shared<T> 타입이 아닙니다.", e.owner);
                    continue;
                }

                var sharedInstance = sharedField.GetValue(e.owner);
                if (sharedInstance == null)
                    continue;

                var valueField = sharedField.FieldType.GetField(
                    "value",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (valueField == null)
                {
                    Debug.LogWarning($"Shared<T> 내부에 'value' 필드를 찾지 못했습니다. (필드: {e.sharedFieldName})", e.owner);
                    continue;
                }

                var vmPath = e.binding.vmPath;
                var dir    = e.binding.direction;

                // VM → Shared
                if (dir == VmBindingDirection.VmToShared || dir == VmBindingDirection.TwoWay)
                {
                    Action<object> cb = v =>
                    {
                        try
                        {
                            if (v != null && !valueField.FieldType.IsInstanceOfType(v))
                                return;

                            valueField.SetValue(sharedInstance, v);
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex, e.owner);
#endif
                        }
                    };

                    var token = ctx.RegisterListener(vmPath, cb);
                    _subscriptions.Add((vmPath, cb, token));

                    // 초기 값 동기화 (VM → Shared)
                    var current = ctx.GetValue(vmPath);
                    cb(current);
                }

                // Shared → VM
                if (dir == VmBindingDirection.SharedToVm || dir == VmBindingDirection.TwoWay)
                {
                    var localValue = valueField.GetValue(sharedInstance);
                    ctx.SetValue(vmPath, localValue);
                }
            }
        }
    }
}
