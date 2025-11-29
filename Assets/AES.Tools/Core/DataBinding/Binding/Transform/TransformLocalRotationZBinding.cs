// 4) TransformLocalRotationZBinding.cs (2D 회전용)
using UnityEngine;

namespace AES.Tools
{
    public sealed class TransformLocalRotationZBinding : ContextBindingBase
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
            if (value is float angle)
            {
                var euler = transform.localEulerAngles;
                euler.z = angle;
                transform.localEulerAngles = euler;
            }
        }
    }
}