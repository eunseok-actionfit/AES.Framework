using UnityEngine;
using UnityEngine.UI;

namespace AES.Tools.Bindings
{
    [RequireComponent(typeof(Slider))]
    public class SliderBinding : ContextBindingBase
    {
        [SerializeField] Slider target;

        IBindingContext _ctx;
        object _listenerToken;
        bool _isUpdatingFromUI;

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

            _listenerToken = context.RegisterListener(path, OnModelChanged);
            target.onValueChanged.AddListener(OnSliderChanged);
        }

        protected override void OnContextUnavailable()
        {
            if (_ctx != null && _listenerToken != null)
            {
                _ctx.RemoveListener(ResolvedPath, OnModelChanged, _listenerToken);
            }

            if (target != null)
                target.onValueChanged.RemoveListener(OnSliderChanged);

            _ctx = null;
            _listenerToken = null;
        }

        void OnModelChanged(object value)
        {
            float f = 0f;

            if (value is float fv)
                f = fv;
            else if (value != null && float.TryParse(value.ToString(), out var parsed))
                f = parsed;

            _isUpdatingFromUI = true;
            target.value = f;
            _isUpdatingFromUI = false;
        }

        void OnSliderChanged(float value)
        {
            if (_ctx == null || _isUpdatingFromUI)
                return;

            _ctx.SetValue(ResolvedPath, value);
        }
    }
}