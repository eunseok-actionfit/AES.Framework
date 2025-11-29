
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace AES.Tools
{
    /// <summary>
    /// IObservableList 를 감시하여 요소마다 itemPrefab(MonoContextHolder)을 스폰/제거.
    /// VM 인스턴스 기준 diff.
    /// </summary>
    public sealed class ListSpawnBinding : ContextBindingBase
    {
        [SerializeField] private Transform root;
        [SerializeField] private MonoContext itemPrefab;

        [Header( "Events" )]
        [SerializeField] public UnityEvent OnListChangedEvent = new();

        private System.Action<object> _listener;
        private object _token;

        private readonly Dictionary<object, MonoContext> _vmToInstance = new();

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnListChanged;
            _token    = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _listener, _token);

            ClearAll();
        }

        private void OnListChanged(object value)
        {
#if UNITY_EDITOR
            Debug_SetLastValue(value);
#endif
            if (value is not IObservableList list)
                return;

            ApplyDiff(list);
            OnListChangedEvent.Invoke();
        }

        private void ApplyDiff(IObservableList list)
        {
            // 제거 대상 찾기
            var toRemove = new List<object>();
            foreach (var kvp in _vmToInstance)
            {
                bool exists = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (ReferenceEquals(list.GetItem(i), kvp.Key))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    toRemove.Add(kvp.Key);
            }

            // 제거
            foreach (var vm in toRemove)
            {
                if (_vmToInstance.TryGetValue(vm, out var inst) && inst != null)
                    Destroy(inst.gameObject);
                _vmToInstance.Remove(vm);
            }

            // 추가 + 순서 정렬
            int siblingIndex = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var vm = list.GetItem(i);
                if (vm == null) continue;

                if (!_vmToInstance.TryGetValue(vm, out var inst) || inst == null)
                {
                    inst = Object.Instantiate(itemPrefab, root);
                    inst.SetViewModel(vm);
                    _vmToInstance[vm] = inst;
                }

                inst.transform.SetSiblingIndex(siblingIndex++);
            }
        }

        private void ClearAll()
        {
            foreach (var kvp in _vmToInstance)
            {
                if (kvp.Value != null)
                    Object.Destroy(kvp.Value.gameObject);
            }
            _vmToInstance.Clear();
        }
    }
}
