using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VContainer;
using VContainer.Unity;

namespace AES.Tools
{
    // 뷰가 재사용될 때 상태를 초기화하고 싶으면 구현
    public interface IResettableView
    {
        void ResetView();
    }

    public sealed class ListSpawnBinding : ContextBindingBase
    {
        [Header("Root")]
        [SerializeField] private Transform root;

        [Header("Default Prefab")]
        [SerializeField] private MonoContext itemPrefab;

        [Header("Value Converter (VM -> Prefab)")]
        [SerializeField] private bool useConverter = false;
        [SerializeField] private ValueConverterSOBase converter;
        [SerializeField] private string converterParameter;

        [Header("Events")]
        [SerializeField] public UnityEvent OnSpawnEvent = new();
        [SerializeField] public UnityEvent OnDespawnEvent = new();

        [Inject] private IObjectResolver _resolver;

        private Action<object> _listener;
        private object _token;

        // VM -> 인스턴스 매핑
        private readonly Dictionary<object, MonoContext> _vmToInstance = new();

        // VM -> 이번에 사용 중인 Prefab 매핑 (프리팹 변경 감지용)
        private readonly Dictionary<object, MonoContext> _vmToPrefab = new();

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnListChanged;
            _token = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (_token is IDisposable d) { d.Dispose(); }
            else if (BindingContext != null && _listener != null) { BindingContext.RemoveListener(ResolvedPath, _token); }

            _listener = null;
            _token = null;

            ClearAll();
        }

        private void OnListChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (value is not IObservableList list)
            {
                // 리스트가 null(또는 다른 타입)이 되면 뷰를 전부 정리
                ClearAll();
                return;
            }

            ApplyHybridBinding(list);
        }

        /// <summary>
        /// 하이브리드 방식:
        /// - VM 기준으로 인스턴스를 찾고
        /// - 최종 Transform 순서는 인덱스 기준으로 정렬.
        /// </summary>
        private void ApplyHybridBinding(IObservableList list)
        {
            if (root == null)
                return;

            int count = list.Count;

            var usedInstances = new HashSet<MonoContext>();

            for (int i = 0; i < count; i++)
            {
                var vm = list.GetItem(i);

                if (vm == null)
                    continue;

                // 이번 VM에 대해 어떤 prefab을 써야 하는지 결정
                var desiredPrefab = ResolvePrefabForVm(vm);
                if (desiredPrefab == null)
                    continue;

                // 기존 인스턴스가 있으면 prefab 변경 여부 확인
                if (_vmToInstance.TryGetValue(vm, out var existingCtx) && existingCtx != null)
                {
                    if (_vmToPrefab.TryGetValue(vm, out var usedPrefab) && usedPrefab != null)
                    {
                        // prefab이 바뀌어야 하면 교체
                        if (usedPrefab != desiredPrefab)
                        {
                            Destroy(existingCtx.gameObject);
                            OnDespawnEvent.Invoke();

                            _vmToInstance.Remove(vm);
                            existingCtx = null;
                        }
                    }
                }

                // 없으면 새로 생성
                if (!_vmToInstance.TryGetValue(vm, out var ctx) || ctx == null)
                {
                    ctx = InstantiatePrefab(desiredPrefab);
                    if (ctx == null)
                        continue;

                    _vmToInstance[vm] = ctx;
                    _vmToPrefab[vm] = desiredPrefab;
                    OnSpawnEvent.Invoke();
                }
                else
                {
                    // 기존 인스턴스 유지 시에도 prefab 기록 갱신(안전)
                    _vmToPrefab[vm] = desiredPrefab;
                }

                ResetViewIfPossible(ctx);
                ctx.SetViewModel(vm);

                var tr = ctx.transform;

                if (tr.parent != root)
                    tr.SetParent(root, false);

                tr.SetSiblingIndex(i);

                usedInstances.Add(ctx);
            }

            // 이번 리스트에 없는 VM/GO 정리
            var vmKeysToRemove = new List<object>();

            foreach (var kvp in _vmToInstance)
            {
                var vm = kvp.Key;
                var ctx = kvp.Value;

                if (ctx == null || !usedInstances.Contains(ctx))
                {
                    if (ctx != null)
                    {
                        Destroy(ctx.gameObject);
                        OnDespawnEvent.Invoke();
                    }

                    vmKeysToRemove.Add(vm);
                }
            }

            foreach (var key in vmKeysToRemove)
            {
                _vmToInstance.Remove(key);
                _vmToPrefab.Remove(key);
            }

            // root 아래에 있는데 usedInstances에 없는 (이상한) 자식 정리
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                var ctx = child.GetComponent<MonoContext>();

                if (ctx == null)
                    continue;

                if (!usedInstances.Contains(ctx))
                {
                    Destroy(child.gameObject);
                    OnDespawnEvent.Invoke();
                }
            }
        }

        private MonoContext ResolvePrefabForVm(object vm)
        {
            var prefab = itemPrefab;

            if (useConverter && converter != null)
            {
                var converted = converter.Convert(vm, typeof(MonoContext), converterParameter, null);
                if (converted is MonoContext mc && mc != null)
                    prefab = mc;
            }

            return prefab;
        }

        private MonoContext InstantiatePrefab(MonoContext prefab)
        {
            if (prefab == null || root == null)
                return null;

            if (_resolver == null)
                return Instantiate(prefab, root);

            return _resolver.Instantiate(prefab, root);
        }

        private void ResetViewIfPossible(MonoContext ctx)
        {
            if (ctx == null)
                return;

            var resettable = ctx.GetComponent<IResettableView>();
            if (resettable != null)
                resettable.ResetView();
        }

        private void ClearAll()
        {
            foreach (var kvp in _vmToInstance)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }

            _vmToInstance.Clear();
            _vmToPrefab.Clear();

            if (root != null)
            {
                for (int i = root.childCount - 1; i >= 0; i--)
                    Destroy(root.GetChild(i).gameObject);
            }
        }
    }
}
