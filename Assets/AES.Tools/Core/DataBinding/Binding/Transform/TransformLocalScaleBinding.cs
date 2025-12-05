// 3) TransformLocalScaleBinding.cs
using UnityEngine;

namespace AES.Tools
{
    public sealed class TransformLocalScaleBinding : ContextBindingBase
    {
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
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (value is Vector3 v3)
                transform.localScale = v3;
            else if (value is float s)
                transform.localScale = new Vector3(s, s, 1f);
        }
    }
}