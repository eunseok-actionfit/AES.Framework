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

    IBindingContext _ctx;
    object _listenerToken;
    bool _isUpdatingFromUI;

    void OnValidate()
    {
        toggle ??= GetComponent<Toggle>();
    }

    protected override void OnContextAvailable(IBindingContext context, string path)
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        if (toggle == null)
        {
            LogBindingError("ToggleBinding: Toggle 이 설정되지 않았습니다.");
            return;
        }

        _ctx = context;

        _listenerToken = context.RegisterListener(path, OnSourceValueChanged);
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    protected override void OnContextUnavailable()
    {
        if (_ctx != null && _listenerToken != null)
        {
            _ctx.RemoveListener(ResolvedPath, _listenerToken);
        }

        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);

        _ctx = null;
        _listenerToken = null;
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
            isOn = value is bool b && b;
        }

        _isUpdatingFromUI = true;
        toggle.isOn = isOn;
        _isUpdatingFromUI = false;
    }

    void OnToggleValueChanged(bool isOn)
    {
        if (_ctx == null || _isUpdatingFromUI)
            return;

        object newValue = isOn;

        if (useConverter && converter != null)
        {
            try
            {
                newValue = converter.ConvertBack(isOn, null, converterParameter, null);
            }
            catch (NotSupportedException)
            {
                // 단방향 컨버터면 무시
            }
        }

        _ctx.SetValue(ResolvedPath, newValue);
    }
}
