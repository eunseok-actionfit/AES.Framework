using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools
{
    public class ValidationTextBinding : ContextBindingBase
    {

        [SerializeField] TMP_Text tmpText;

        IValidatableProperty _validatable;

        protected override void Subscribe()
        {
            var prop = ResolveObservablePropertyBoxed();

            if (prop is IValidatableProperty validatable)
            {
                _validatable = validatable;
                _validatable.OnValidationChanged += OnValidationChanged;
                UpdateText();
            }
            else { LogBindingError($"멤버 '{memberPath}' 는 IValidatableProperty 를 구현하지 않습니다."); }
        }

        protected override void Unsubscribe()
        {
            if (_validatable != null)
            {
                _validatable.OnValidationChanged -= OnValidationChanged;
                _validatable = null;
            }
        }

        void OnValidationChanged(IValidatableProperty _)
        {
            UpdateText();
        }

        void UpdateText()
        {
            string msg = string.Empty;

            if (_validatable != null && _validatable.HasError)
                msg = _validatable.ValidationError.Message;


            if (tmpText != null)
                tmpText.text = msg;
        }
    }
}