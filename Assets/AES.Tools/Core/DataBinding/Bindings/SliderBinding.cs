using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools.Bindings
{
    [RequireComponent(typeof(Slider))]
    public class SliderBinding : ContextBindingBase
    {
        [SerializeField] private Slider target;

        private ObservableProperty<float> _prop;
        private bool _isUpdatingFromUI;
        
        private void Reset() => target = GetComponent<Slider>();

        protected override void Subscribe()
        {
            _prop = ResolveObservableProperty<float>();
            if (_prop == null || target == null)
                return;

            _prop.OnValueChanged += OnModelChangedBoxed;
            target.value = _prop.Value;

            target.onValueChanged.AddListener(OnSliderChanged);
        }

        protected override void Unsubscribe()
        {
            if (_prop != null)
                _prop.OnValueChanged -= OnModelChangedBoxed;
            
            
            if (target != null)
                target.onValueChanged.RemoveListener(OnSliderChanged);
        }

        private void OnModelChangedBoxed(float value)
        {
            _isUpdatingFromUI = true;
            target.value = value;
            _isUpdatingFromUI = false;
        }

        private void OnSliderChanged(float value)
        {
            if (_isUpdatingFromUI || _prop == null)
                return;

            _prop.Value = value;
        }
    }
}