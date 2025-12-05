using System;
using UnityEngine;
using UnityEngine.UI;

namespace AES.Tools
{
    [RequireComponent(typeof(Image))]
    public sealed class ImageColorBinding : ContextBindingBase
    {
        [SerializeField] private Image image;

        [Header("Value Converter")]
        [SerializeField] bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] ValueConverterSOBase converter;
        [SerializeField, ShowIf(nameof(useConverter))] string converterParameter;

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
                BindingContext.RemoveListener(ResolvedPath, _listener, _token);
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (image == null)
                return;

            if (useConverter && converter != null)
            {
                value = converter.Convert(
                    value,
                    typeof(Color),
                    converterParameter,
                    null 
                );
            }

            if (value is Color c)
                image.color = c;
        }
    }
}