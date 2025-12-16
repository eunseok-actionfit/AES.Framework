using System;
using UnityEngine;
using UnityEngine.UI;

namespace AES.Tools
{
    [RequireComponent(typeof(Image))]
    public sealed class ImageBoolSpriteBinding : ContextBindingBase
    {
        [SerializeField] private Image image;

        [Header("Bool -> Sprite")]
        [SerializeField] private Sprite trueSprite;
        [SerializeField] private Sprite falseSprite;
        [SerializeField] private bool invert;
        [SerializeField] private bool setNativeSize;

        private Action<object> _listener;
        private object _token;

        private void Reset() => image = GetComponent<Image>();

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _token);

            _listener = null;
            _token = null;
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (image == null)
                return;

            if (value is not bool b)
                return;

            if (invert) b = !b;

            var next = b ? trueSprite : falseSprite;
            if (image.sprite == next)
                return;

            image.sprite = next;
            if (setNativeSize && next != null)
                image.SetNativeSize();
        }
    }
}