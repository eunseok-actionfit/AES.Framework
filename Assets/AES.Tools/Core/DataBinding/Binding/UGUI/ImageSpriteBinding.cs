using System;
using UnityEngine;
using UnityEngine.UI;

namespace AES.Tools
{
    [RequireComponent(typeof(Image))]
    public sealed class ImageSpriteBinding : ContextBindingBase
    {
        [SerializeField] private Image image;

        [Header("Value Converter")]
        [SerializeField] private bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] private ValueConverterSOBase converter;
        [SerializeField, ShowIf(nameof(useConverter))] private string converterParameter;

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
            {
                return;
            }
                

            if (useConverter && converter != null)
            {
                value = converter.Convert(
                    value,
                    typeof(Sprite),
                    converterParameter,
                    null
                );
            }

            if (value is Sprite s)
                image.sprite = s;
            else if (value == null)
                image.sprite = null;

            if (image.sprite == null)
                image.enabled = false;
            else if(image.type == Image.Type.Simple)
                image.SetNativeSize();
        }
    }
}