using System;
using AES.Tools;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleBinding : ContextBindingBase
{
    [SerializeField] Toggle toggle;

    [Header("Value Converter")]
    [SerializeField] bool useConverter;
    [SerializeField] ValueConverterSOBase converter;
    [SerializeField] string converterParameter;

    IObservableProperty _property; // 원래 타입 유지
    
    private void OnValidate()
    {
        toggle ??= GetComponent<Toggle>();
    }

    protected override void Subscribe()
    {
        _property = ResolveObservablePropertyBoxed();
        if (_property == null || toggle == null)
            return;

        _property.OnValueChangedBoxed += OnSourceValueChanged;
        toggle.onValueChanged.AddListener(OnToggleValueChanged);

        OnSourceValueChanged(_property.Value);
    }

    protected override void Unsubscribe()
    {
        if (_property != null)
        {
            _property.OnValueChangedBoxed -= OnSourceValueChanged;
            _property = null;
        }

        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    void OnSourceValueChanged(object value)
    {
        bool isOn;

        if (useConverter && converter != null)
        {
            var result = converter.Convert(value, typeof(bool), converterParameter, null);
            isOn = result is bool b && b;
        }
        else
        {
            // 기본: 단순 bool 캐스팅
            isOn = value is bool b && b;
        }

        toggle.isOn = isOn;
    }

    void OnToggleValueChanged(bool isOn)
    {
        if (_property == null)
            return;

        object newValue = isOn;

        if (useConverter && converter != null)
        {
            // 양방향 지원하는 컨버터라면 ConvertBack 사용
            try
            {
                newValue = converter.ConvertBack(isOn, _property.ValueType, converterParameter, null);
            }
            catch (NotSupportedException)
            {
                // 단방향 컨버터면 무시
            }
        }

        _property.SetBoxedValue(newValue);
    }
}