// 1) TransformPositionBinding.cs
using UnityEngine;

namespace AES.Tools
{
    public sealed class TransformPositionBinding : ContextBindingBase
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
                transform.position = v3;
            else if (value is Vector2 v2)
                transform.position = new Vector3(v2.x, v2.y, transform.position.z);
        }
    }
}