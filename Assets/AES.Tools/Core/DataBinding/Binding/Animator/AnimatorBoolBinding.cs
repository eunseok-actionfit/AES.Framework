// AnimatorBoolBinding.cs
using UnityEngine;


namespace AES.Tools
{
    [RequireComponent(typeof(UnityEngine.Animator))]
    public sealed class AnimatorBoolBinding : ContextBindingBase
    {
        [SerializeField] private string parameterName;

        private UnityEngine.Animator _animator;
        private System.Action<object> _listener;
        private object _token;

        private void Awake() => _animator = GetComponent<UnityEngine.Animator>();

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
            if (value is bool b)
                _animator.SetBool(parameterName, b);
        }
    }
}