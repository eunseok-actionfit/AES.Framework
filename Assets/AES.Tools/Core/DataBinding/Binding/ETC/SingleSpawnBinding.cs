// 파일: SingleSpawnBinding.cs
using UnityEngine;

namespace AES.Tools
{
    /// <summary>
    /// path에 CatViewModel 같은 단일 객체가 들어오면 itemPrefab을 0 또는 1개 유지.
    /// </summary>
    public sealed class SingleSpawnBinding : ContextBindingBase
    {
        [SerializeField] private Transform root;
        [SerializeField] private MonoContext itemPrefab;

        private System.Action<object> _listener;
        private object _token;
        private MonoContext _instance;
        private object _currentVm;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token    = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _listener, _token);

            Clear();
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_SetLastValue(value);
#endif
            if (value == null)
            {
                Clear();
                return;
            }

            if (!ReferenceEquals(value, _currentVm))
            {
                Clear();
                _currentVm = value;
                _instance = Instantiate(itemPrefab, root);
                _instance.SetViewModel(_currentVm);
            }
        }

        private void Clear()
        {
            _currentVm = null;
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}