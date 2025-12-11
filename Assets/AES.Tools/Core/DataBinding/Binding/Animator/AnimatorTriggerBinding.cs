// AnimatorTriggerBinding.cs
using UnityEngine;

namespace AES.Tools
{
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorTriggerBinding : ContextBindingBase
    {
        [SerializeField] private string parameterName;
        [SerializeField] private string fireOnValue;

        private Animator _animator;
        private System.Action<object> _listener;
        private object _token;

        private void Awake() => _animator = GetComponent<Animator>();

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token    = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _token);
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (_animator == null || string.IsNullOrEmpty(parameterName)) return;
            if (value == null) return;

            if (string.IsNullOrEmpty(fireOnValue) || value.ToString() == fireOnValue)
                _animator.SetTrigger(parameterName);
        }
    }
}