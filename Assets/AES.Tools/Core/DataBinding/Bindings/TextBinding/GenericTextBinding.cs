using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// using TMPro;

namespace AES.Tools
{
    public class GenericTextBinding<T> : ContextBindingBase
    {
        [SerializeField] TMP_Text tmpText;

        [Header("Formatting")]
        [SerializeField] bool useFormat;
        [SerializeField] string format;
        [SerializeField] bool useInvariantCulture = true;

        [Header("Value Converter")]
        [SerializeField] bool useConverter;
        [SerializeField] ValueConverterSOBase converter;
        [SerializeField] string converterParameter; // string 으로 넘기고, 컨버터에서 해석

        ObservableProperty<T> _property;

        protected override void Subscribe()
        {
            var boxed = ResolveObservablePropertyBoxed();
            if (boxed == null)
                return;

            if (boxed is ObservableProperty<T> typed)
            {
                _property = typed;
                _property.OnValueChanged += OnValueChanged;
                // 초기값 반영
                OnValueChanged(_property.Value);
            }
            else { LogBindingError($"멤버 '{memberPath}' 는 IObservableProperty<{typeof(T).Name}> 가 아닙니다. 실제 타입: {boxed.GetType().Name}"); }
        }

        protected override void Unsubscribe()
        {
            if (_property != null)
            {
                _property.OnValueChanged -= OnValueChanged;
                _property = null;
            }
        }

        void OnValueChanged(T newValue)
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

            if (tmpText != null)
                tmpText.text = text;
        }
    }
}