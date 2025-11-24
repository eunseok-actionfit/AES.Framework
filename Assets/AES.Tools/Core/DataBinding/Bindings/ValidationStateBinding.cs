using System;
using UnityEngine;

namespace AES.Tools
{
    public class ValidationStateBinding : ContextBindingBase
    {
        [SerializeField] GameObject target;
        [SerializeField] bool activeWhenError = true;

        IValidatableProperty _validatable;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            if (target == null)
                return;

            if (string.IsNullOrEmpty(path))
            {
                LogBindingError("ValidationStateBinding: memberPath 가 비어 있습니다.");
                return;
            }

            try
            {
                // Provider 기반 구조에서는 ViewModel 직접 만지지 않고
                // IBindingContext 를 통해 값 조회
                var value = context.GetValue(path);

                if (value is IValidatableProperty validatable)
                {
                    // 기존 구독 해제
                    if (_validatable != null)
                        _validatable.OnValidationChanged -= OnValidationChanged;

                    _validatable = validatable;
                    _validatable.OnValidationChanged += OnValidationChanged;
                    UpdateState();
                }
                else
                {
                    LogBindingError($"멤버 '{path}' 는 IValidatableProperty 를 구현하지 않습니다.");
                    _validatable = null;
                    UpdateState();
                }
            }
            catch (Exception e)
            {
                LogBindingException($"ValidationStateBinding: Path '{path}' 해석 중 오류", e);
            }
        }

        protected override void OnContextUnavailable()
        {
            if (_validatable != null)
            {
                _validatable.OnValidationChanged -= OnValidationChanged;
                _validatable = null;
            }

            // 컨텍스트 사라지면 기본적으로 비활성화
            if (target != null)
                target.SetActive(false);
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
