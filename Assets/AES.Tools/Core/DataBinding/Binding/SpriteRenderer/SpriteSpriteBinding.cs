// SpriteSpriteBinding.cs
using UnityEngine;

namespace AES.Tools
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteSpriteBinding : ContextBindingBase
    {
        private SpriteRenderer _renderer;
        private System.Action<object> _listener;
        private object _token;

        private void Awake() => _renderer = GetComponent<SpriteRenderer>();

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
            if (_renderer == null) return;
            if (value is Sprite s)
                _renderer.sprite = s;
        }
    }
}