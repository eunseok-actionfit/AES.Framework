// 2) TransformLocalPositionBinding.cs
using UnityEngine;

namespace AES.Tools
{
    public sealed class TransformLocalPositionBinding : ContextBindingBase
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
            Debug_SetLastValue(value);
#endif
            if (value is Vector3 v3)
                transform.localPosition = v3;
            else if (value is Vector2 v2)
                transform.localPosition = new Vector3(v2.x, v2.y, transform.localPosition.z);
        }
    }
}