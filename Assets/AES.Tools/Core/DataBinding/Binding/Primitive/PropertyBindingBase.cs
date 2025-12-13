// PropertyBindingBase.cs
using System;
using UnityEngine;
using UnityEngine.Events;


namespace AES.Tools
{
    public abstract class PropertyBindingBase<T> : ContextBindingBase
    {
        [SerializeField] private UnityEvent<T> onChanged;

        private Action<object> _listener;
        private object _token;

        protected override void OnContextAvailable(IBindingContext ctx, string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            _listener = v =>
            {
                if (v is T value)
                {
                    OnValueChanged(value);
                }
            };

            _token = ctx.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _token);
        }

        protected virtual void OnValueChanged(T value)
        {
            onChanged?.Invoke(value);
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
        }
    }
}