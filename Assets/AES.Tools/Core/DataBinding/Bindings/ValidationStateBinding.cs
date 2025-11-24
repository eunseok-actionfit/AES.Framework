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

            var ctx = CurrentContext;
            var vm = ctx?.ViewModel;
            if (vm == null)
            {
                LogBindingError("ValidationStateBinding: ViewModel 을 찾지 못했습니다.");
                return;
            }

            try
            {
                var mp = MemberPathCache.Get(vm.GetType(), path);
                var value = mp.GetValue(vm);

                if (value is IValidatableProperty validatable)
                {
                    _validatable = validatable;
                    _validatable.OnValidationChanged += OnValidationChanged;
                    UpdateState();
                }
                else
                {
                    LogBindingError($"멤버 '{path}' 는 IValidatableProperty 를 구현하지 않습니다.");
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
