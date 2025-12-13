using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools
{
    [RequireComponent(typeof(Slider))]
    public class SliderBinding : ContextBindingBase
    {
        [SerializeField] Slider target;

        [Header("Value Converter")]
        [SerializeField] bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] ValueConverterSOBase converter;
        // parameter 자리에 slider 현재 값 넣는 형태 유지
        //[SerializeField, ShowIf(nameof(useConverter))] string converterParameter;

        IBindingContext _ctx;
        object _listenerToken;
        bool _isUpdatingFromUI;

        Type _modelValueType; // ViewModel 실제 값 타입 (float / int / 등)

        void OnValidate()
        {
            target ??= GetComponent<Slider>();
        }

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            if (target == null)
                target = GetComponent<Slider>();

            if (target == null)
            {
                LogBindingError("SliderBinding: Slider 가 설정되지 않았습니다.");
                return;
            }

            _ctx = context;

            // 최초 값 타입 한 번 확인 (ObservableProperty 대응)
            var initial = context.GetValue(path);
            if (initial is IObservableProperty op)
                _modelValueType = op.ValueType;
            else
                _modelValueType = initial?.GetType();

            _listenerToken = context.RegisterListener(path, OnModelChanged);
            target.onValueChanged.AddListener(OnSliderChanged);
        }

        protected override void OnContextUnavailable()
        {
            if (_ctx != null && _listenerToken != null)
            {
                _ctx.RemoveListener(ResolvedPath, _listenerToken);
            }

            if (target != null)
                target.onValueChanged.RemoveListener(OnSliderChanged);

            _ctx = null;
            _listenerToken = null;
            _modelValueType = null;
        }

        // 모델 → UI
        void OnModelChanged(object value)
        {
            if (_modelValueType == null && value != null)
                _modelValueType = value.GetType();

            float f = 0f;

            if (useConverter && converter != null)
            {
                var culture = CultureInfo.InvariantCulture;
                object converted = converter.Convert(value, typeof(float), target.value, culture);

                if (converted is float fv)
                    f = fv;
                else if (converted != null &&
                         float.TryParse(converted.ToString(), NumberStyles.Float, culture, out var parsedF))
                    f = parsedF;
            }
            else
            {
                if (value is float fv)
                    f = fv;
                else if (value != null && float.TryParse(value.ToString(), out var parsed))
                    f = parsed;
            }

            _isUpdatingFromUI = true;
            target.value = f;
            _isUpdatingFromUI = false;
        }

        // UI → 모델
        void OnSliderChanged(float value)
        {
            if (_ctx == null || _isUpdatingFromUI)
                return;

            object newValue = value;

            if (useConverter && converter != null)
            {
                var targetType = _modelValueType ?? typeof(float);
                var culture = CultureInfo.InvariantCulture;

                var convertedBack = converter.ConvertBack(value, targetType, target.value, culture);

                if (convertedBack == null)
                {
                    newValue = targetType.IsValueType
                        ? Activator.CreateInstance(targetType)
                        : null;
                }
                else
                {
                    newValue = convertedBack;
                }
            }

            _ctx.SetValue(ResolvedPath, newValue);
        }
    }
}
