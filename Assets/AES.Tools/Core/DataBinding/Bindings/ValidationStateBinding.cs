using UnityEngine;

namespace AES.Tools
{
    public class ValidationStateBinding : ContextBindingBase
    {
        [SerializeField] GameObject target;
        [SerializeField] bool activeWhenError = true;

        IValidatableProperty _validatable;

        protected override void Subscribe()
        {
            var prop = ResolveObservablePropertyBoxed();
            if (prop is IValidatableProperty validatable)
            {
                _validatable = validatable;
                _validatable.OnValidationChanged += OnValidationChanged;
                UpdateState();
            }
            else
            {
                LogBindingError($"멤버 '{memberPath}' 는 IValidatableProperty 를 구현하지 않습니다.");
            }
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
            UpdateState();
        }

        void UpdateState()
        {
            if (target == null)
                return;

            if (_validatable == null)
            {
                target.SetActive(false);
                return;
            }

            bool isError = _validatable.HasError;
            target.SetActive(activeWhenError ? isError : !isError);
        }
    }
}