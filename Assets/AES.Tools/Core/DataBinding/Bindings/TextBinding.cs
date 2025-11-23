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
        [SerializeField] bool useFormat;
        [SerializeField, ShowIf(nameof(useFormat))] string format;
        [SerializeField, ShowIf(nameof(useFormat))] bool useInvariantCulture = true;

        [Header("Value Converter")]
        [SerializeField] bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] ValueConverterSOBase converter;
        [SerializeField, ShowIf(nameof(useConverter))] string converterParameter; // string 으로 넘기고, 컨버터에서 해석

        IObservableProperty _property;

        private void OnValidate()
        {
            tmpText ??= GetComponent<TMP_Text>();
        }

        protected override void Subscribe()
        {
            _property = ResolveObservablePropertyBoxed();
            if (_property == null || tmpText == null)
                return;

            _property.OnValueChangedBoxed += OnValueChanged;
            OnValueChanged(_property.Value);
        }

        protected override void Unsubscribe()
        {
            if (_property != null)
            {
                _property.OnValueChangedBoxed -= OnValueChanged;
                _property = null;
            }
        }

        void OnValueChanged(object newValue)
        {
            var culture = useInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;
            string text;

            if (useConverter && converter != null)
            {
                var display = converter.Convert(newValue, typeof(string), converterParameter, culture);
                text = display?.ToString() ?? string.Empty;
            }
            else if (useFormat) { text = TextFormatHelper.Format(newValue, true, format, culture); }
            else { text = TextFormatHelper.ConvertToString(newValue, culture); }


            tmpText.text = text;
            
#if UNITY_EDITOR
            Debug_SetLastValue(text); 
#endif
        }
    }
}