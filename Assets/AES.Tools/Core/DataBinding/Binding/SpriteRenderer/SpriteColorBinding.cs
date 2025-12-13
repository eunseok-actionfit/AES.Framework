using UnityEngine;


namespace AES.Tools
{
    [RequireComponent(typeof(UnityEngine.SpriteRenderer))]
    public sealed class SpriteColorBinding : ContextBindingBase
    {
        [SerializeField] private UnityEngine.SpriteRenderer renderer;
        
        [Header("Value Converter")]
        [SerializeField] bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] ValueConverterSOBase converter;
        [SerializeField, ShowIf(nameof(useConverter))] string converterParameter;

        private System.Action<object> _listener;
        private object _token;

        private void Reset() => renderer = GetComponent<UnityEngine.SpriteRenderer>();

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _token);
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (renderer == null)
                return;

            if (useConverter && converter != null)
            {
                value = converter.Convert(
                    value,
                    typeof(Color),
                    converterParameter,
                    null // 문화권 필요 없음
                );
            }

            if (value is Color c)
                renderer.color = c;
        }
    }
}