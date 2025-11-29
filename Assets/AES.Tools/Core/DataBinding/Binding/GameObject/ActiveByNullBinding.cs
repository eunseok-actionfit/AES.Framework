// ActiveByNullBinding.cs
using UnityEngine;

namespace AES.Tools
{
    /// <summary>
    /// path의 값이 null이면 비활성, null이 아니면 활성 (또는 반대로).
    /// </summary>
    public sealed class ActiveByNullBinding : ContextBindingBase
    {
        [SerializeField] private bool activeWhenNull = false;

        private System.Action<object> _listener;
        private object _token;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token    = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _listener, _token);
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_SetLastValue(value);
#endif
            bool isNull = value == null;
            gameObject.SetActive(activeWhenNull ? isNull : !isNull);
        }
    }
}