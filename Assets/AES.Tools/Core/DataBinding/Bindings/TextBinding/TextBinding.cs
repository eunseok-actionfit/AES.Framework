using TMPro;
using UnityEngine;


namespace AES.Tools
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TextBinding : ContextBindingBase
    {
        [Header("Target")]
        [SerializeField] private TMP_Text targetText;

        [Header("Formatting")]
        [SerializeField] private bool useFormat; // 포맷 쓸지 여부
        [ShowIf(nameof(useFormat))]
        [SerializeField] private string format = "{0}"; // string.Format 패턴

        [Tooltip("true일 경우 InvariantCulture 사용, 아니면 CurrentCulture 사용")]
        [SerializeField] private bool useInvariantCulture = true;

        private IObservableProperty _prop;

        private void Reset()
        {
            targetText = GetComponent<TMP_Text>();
        }

        protected override void Subscribe()
        {
            if (targetText == null)
            {
                Debug.LogError($"{GetType().Name}: targetText 가 설정되지 않았습니다.", this);
                return;
            }

            _prop = ResolveObservablePropertyBoxed();
            if (_prop == null)
                return;

            _prop.OnValueChangedBoxed += OnValueChanged;
            OnValueChanged(_prop.Value); 
        }

        protected override void Unsubscribe()
        {
            if (_prop != null)
            {
                _prop.OnValueChangedBoxed -= OnValueChanged;
                _prop = null;
            }
        }

        private void OnValueChanged(object value)
        {
            if (targetText == null) return;

            var provider = useInvariantCulture
                ? System.Globalization.CultureInfo.InvariantCulture
                : System.Globalization.CultureInfo.CurrentCulture;
            
            var final = TextFormatHelper.Format(value, useFormat, format, provider);

            targetText.text = final;
        }
    }
}