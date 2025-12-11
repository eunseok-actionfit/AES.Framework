using TMPro;
using UnityEngine;

namespace AES.Tools
{
    /// <summary>
    /// IValidatableProperty.ValidationError.Message 를 TMP_Text 에 표시하는 바인딩.
    /// ObservableProperty 기반, INPC 기반 ViewModel 모두 지원.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class ValidationTextBinding : ContextBindingBase
    {
        [SerializeField] TMP_Text tmpText;

        IBindingContext _ctx;
        object _listenerToken;
        IValidatableProperty _validatable;

        private void OnValidate()
        {
            tmpText ??= GetComponent<TMP_Text>();
        }

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _ctx = context;
            if (tmpText == null)
                tmpText = GetComponent<TMP_Text>();

            _listenerToken = context.RegisterListener(path, OnValueChanged);
        }

        protected override void OnContextUnavailable()
        {
            if (_ctx != null && _listenerToken != null)
            {
                _ctx.RemoveListener(ResolvedPath, _listenerToken);
            }

            _ctx = null;
            _listenerToken = null;
            _validatable = null;
        }

        void OnValueChanged(object value)
        {
            _validatable = value as IValidatableProperty;
            UpdateText();
        }

        void UpdateText()
        {
            if (tmpText == null)
                return;

            string msg = string.Empty;

            if (_validatable != null && _validatable.HasError)
                msg = _validatable.ValidationError.Message;

            tmpText.text = msg;

#if UNITY_EDITOR
            Debug_OnValueUpdated(msg, ResolvedPath);
#endif
        }
    }
}