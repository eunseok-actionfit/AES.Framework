using System.Globalization;
using TMPro;
using UnityEngine;

namespace AES.Tools
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextBinding : ContextBindingBase
    {
        [SerializeField] TMP_Text tmpText;

        [Header("Formatting")]
        [SerializeField] bool useFormat = true;
        // 기본값은 "{0}" → 그냥 값만 출력
        [SerializeField, ShowIf(nameof(useFormat))] string format = "{0}";
        [SerializeField, ShowIf(nameof(useFormat))] bool useInvariantCulture = true;

        [Header("Value Converter")]
        [SerializeField] bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] ValueConverterSOBase converter;
        [SerializeField, ShowIf(nameof(useConverter))] string converterParameter;

        object _listenerToken;
        IBindingContext _ctx;

        private void OnValidate()
        {
            tmpText ??= GetComponent<TMP_Text>();
        }

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            if (tmpText == null)
                tmpText = GetComponent<TMP_Text>();

            _ctx = context;
            _listenerToken = context.RegisterListener(path, OnValueChanged);
        }

        protected override void OnContextUnavailable()
        {
            if (_ctx != null && _listenerToken != null)
                _ctx.RemoveListener(ResolvedPath, OnValueChanged, _listenerToken);

            _listenerToken = null;
            _ctx = null;
        }

        void OnValueChanged(object rawValue)
        {
            var culture = useInvariantCulture
                ? CultureInfo.InvariantCulture
                : CultureInfo.CurrentCulture;

            object value = rawValue;

            if (useConverter && converter != null)
                value = converter.Convert(value, typeof(object), converterParameter, culture);

            string text = TextFormatHelper.Format(value, useFormat, format, culture);

            tmpText.text = text;

#if UNITY_EDITOR
            Debug_OnValueUpdated(text, ResolvedPath);
#endif
        }
    }
}
