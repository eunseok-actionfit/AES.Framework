using System.Globalization;
using TMPro;
using UnityEngine;

namespace AES.Tools.Bindings
{
    [RequireComponent(typeof(TMP_InputField))]
    public class InputFieldBinding : ContextBindingBase
    {
        [SerializeField] TMP_InputField input;

        [Header("Value Type")]
        [SerializeField] ParameterType valueType = ParameterType.String;

        [Header("Update Behaviour")]
        [SerializeField] bool updateOnEndEditOnly = true;

        [Header("Formatting")]
        [SerializeField] bool useFormat;
        [SerializeField, ShowIf(nameof(useFormat))] string format;
        [SerializeField, ShowIf(nameof(useFormat))] bool useInvariantCulture = true;

        [Header("Value Converter")]
        [SerializeField] bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] ValueConverterSOBase converter;
        [SerializeField, ShowIf(nameof(useConverter))] string converterParameter;

        IBindingContext _ctx;
        object _listenerToken;
        bool _isUpdatingFromUI;

        void OnValidate()
        {
            input ??= GetComponent<TMP_InputField>();
        }

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            if (input == null)
                input = GetComponent<TMP_InputField>();

            if (input == null)
            {
                LogBindingError("InputFieldBinding: TMP_InputField 가 설정되지 않았습니다.");
                return;
            }

            _ctx = context;

            // 모델 → UI
            _listenerToken = context.RegisterListener(path, OnModelChanged);

            // UI → 모델
            if (updateOnEndEditOnly)
                input.onEndEdit.AddListener(OnInputChanged);
            else
                input.onValueChanged.AddListener(OnInputChanged);
        }

        protected override void OnContextUnavailable()
        {
            if (_ctx != null && _listenerToken != null)
                _ctx.RemoveListener(ResolvedPath, OnModelChanged, _listenerToken);

            if (input != null)
            {
                input.onEndEdit.RemoveListener(OnInputChanged);
                input.onValueChanged.RemoveListener(OnInputChanged);
            }

            _ctx = null;
            _listenerToken = null;
        }

        // 모델 → UI
        void OnModelChanged(object value)
        {
            var culture = useInvariantCulture
                ? CultureInfo.InvariantCulture
                : CultureInfo.CurrentCulture;

            string text;
            if (useFormat)
                text = TextFormatHelper.Format(value, true, format, culture);
            else
                text = TextFormatHelper.ConvertToString(value, culture);

            _isUpdatingFromUI = true;
            input.text = text;
            _isUpdatingFromUI = false;

#if UNITY_EDITOR
            Debug_OnValueUpdated(text, ResolvedPath);
#endif
        }

        // UI → 모델
        void OnInputChanged(string text)
        {
            if (_ctx == null || _isUpdatingFromUI)
                return;

            object newValue = ParseByParameterType(text, valueType, out bool parsed);

            if (!parsed)
            {
                Debug.LogWarning(
                    $"InputFieldBinding: 값 파싱 실패 '{text}' (type: {valueType})",
                    this);
                return;
            }

            _ctx.SetValue(ResolvedPath, newValue);
        }

        object ParseByParameterType(string text, ParameterType type, out bool success)
        {
            success = false;

            if (string.IsNullOrEmpty(text))
            {
                if (type == ParameterType.String || type == ParameterType.None)
                {
                    success = true;
                    return string.Empty;
                }

                return null;
            }

            var culture = useInvariantCulture
                ? CultureInfo.InvariantCulture
                : CultureInfo.CurrentCulture;

            switch (type)
            {
                case ParameterType.None:
                case ParameterType.String:
                    success = true;
                    return text;

                case ParameterType.Int:
                    if (int.TryParse(text, NumberStyles.Integer, culture, out var i))
                    {
                        success = true;
                        return i;
                    }
                    return null;

                case ParameterType.Float:
                    if (float.TryParse(text,
                            NumberStyles.Float | NumberStyles.AllowThousands,
                            culture, out var f))
                    {
                        success = true;
                        return f;
                    }
                    return null;

                case ParameterType.Bool:
                    if (bool.TryParse(text, out var b))
                    {
                        success = true;
                        return b;
                    }
                    if (text == "0")
                    {
                        success = true;
                        return false;
                    }
                    if (text == "1")
                    {
                        success = true;
                        return true;
                    }
                    return null;

                default:
                    return null;
            }
        }
    }
}
