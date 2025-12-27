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
        [SerializeField] private Transform   root;
        [SerializeField] private MonoContext itemPrefab;

        [Header("Events")]
        [SerializeField] public UnityEvent OnSpawnEvent   = new();
        [SerializeField] public UnityEvent OnDespawnEvent = new();
        
        [Inject] private IObjectResolver _resolver;

        private Action<object> _listener;
        private object         _token;

        // VM -> 인스턴스 매핑
        private readonly Dictionary<object, MonoContext> _vmToInstance = new();

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnListChanged;
            _token    = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (_token is IDisposable d)
            {
                d.Dispose();
            }
            else if (BindingContext != null && _listener != null)
            {
                BindingContext.RemoveListener(ResolvedPath, _token);
            }

            _listener = null;
            _token    = null;

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
            if (root == null || itemPrefab == null)
                return;

            int count = list.Count;

            var usedInstances = new HashSet<MonoContext>();

            for (int i = 0; i < count; i++)
            {
                var vm = list.GetItem(i);
                if (vm == null)
                    continue;

                if (!_vmToInstance.TryGetValue(vm, out var ctx) || ctx == null)
                {
                   // ctx = Instantiate(itemPrefab, root);
                   ctx = _resolver.Instantiate(itemPrefab, root); 
                    _vmToInstance[vm] = ctx;
                    OnSpawnEvent.Invoke();
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
                var ctx = kvp.Value;
                if (ctx == null || !usedInstances.Contains(ctx))
                {
                    if (ctx != null)
                    {
                        Destroy(ctx.gameObject);
                        OnDespawnEvent.Invoke();
                    }
                    vmKeysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in vmKeysToRemove)
                _vmToInstance.Remove(key);

            // root 아래에 있는데 딕셔너리에 없는 (이상한) 자식 정리
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                var ctx   = child.GetComponent<MonoContext>();

                if (ctx == null)
                    continue;

                if (!usedInstances.Contains(ctx))
                {
                    Destroy(child.gameObject);
                    OnDespawnEvent.Invoke();
                }
            }
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

            if (root != null)
            {
                for (int i = root.childCount - 1; i >= 0; i--)
                    Destroy(root.GetChild(i).gameObject);
            }
        }
    }
}
