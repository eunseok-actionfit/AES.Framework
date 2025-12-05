// ActiveBinding.cs
using UnityEngine;

namespace AES.Tools
{
    public sealed class ActiveBinding : ContextBindingBase
    {
        [SerializeField] private bool invert;

        private System.Action<object> _listener;
        private object _token;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token    = context.RegisterListener(path, _listener);
            //OnValueChanged(context.GetValue(path));
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _listener, _token);
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (value is bool b)
                gameObject.SetActive(invert ? !b : b);
        }
    }
}