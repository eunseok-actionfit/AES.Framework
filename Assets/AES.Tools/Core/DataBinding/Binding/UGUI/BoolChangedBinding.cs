using System;
using UnityEngine;
using UnityEngine.Events;

namespace AES.Tools
{
    [Serializable]
    public sealed class BoolChangedEvent : UnityEvent<bool> { }

    public sealed class BoolChangedBinding : ContextBindingBase
    {
        [Header("Converter")]
        [SerializeField] bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] ValueConverterSOBase converter;
        [SerializeField, ShowIf(nameof(useConverter))] string converterParameter;

        [Header("Events")]
        public BoolChangedEvent onValueChanged; // 값 변화 시 bool 값을 넘겨주는 이벤트
        public UnityEvent onTrue;               // false -> true 로 바뀔 때 호출
        public UnityEvent onFalse;              // true -> false 로 바뀔 때 호출

        private Action<object> _listener;
        private object _token;

        private bool _lastValue;
        private bool _initialized = false;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChangedInternal;
            _token = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _listener, _token);
        }

        private void OnValueChangedInternal(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (!TryGetBool(value, out bool current))
                return;

            // 첫 값은 기준값만 저장, 이벤트는 쏘지 않음
            if (!_initialized)
            {
                _initialized = true;
                _lastValue = current;
                return;
            }

            // 값이 바뀐 경우에만 이벤트 발행
            if (_lastValue != current)
            {
                _lastValue = current;

                // 1) bool 값 자체를 넘기는 이벤트
                onValueChanged?.Invoke(current);

                // 2) true / false 전용 이벤트
                if (current)
                    onTrue?.Invoke();
                else
                    onFalse?.Invoke();
            }
        }

        private bool TryGetBool(object value, out bool result)
        {
            // 컨버터 사용
            if (useConverter && converter != null)
            {
                value = converter.Convert(
                    value,
                    typeof(bool),
                    converterParameter,
                    null
                );
            }

            if (value is bool b)
            {
                result = b;
                return true;
            }

            if (value != null && bool.TryParse(value.ToString(), out var parsed))
            {
                result = parsed;
                return true;
            }

            result = false;
            return false;
        }
    }
}
