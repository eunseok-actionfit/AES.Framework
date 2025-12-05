using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using AES.Tools;

[RequireComponent(typeof(TMP_Dropdown))]
public class EnumDropdownBinding : ContextBindingBase
{
    [SerializeField] TMP_Dropdown dropdown;

    IBindingContext _ctx;
    object _token;

    Type _enumType;
    bool _optionsInitialized;
    bool _isUpdatingFromModel;

    void OnValidate()
    {
        dropdown ??= GetComponent<TMP_Dropdown>();
    }

    protected override void OnContextAvailable(IBindingContext context, string path)
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        _ctx = context;

        // VM → UI
        _token = context.RegisterListener(path, OnModelChanged);

        // UI → VM
        dropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    protected override void OnContextUnavailable()
    {
        if (_ctx != null && _token != null)
            _ctx.RemoveListener(ResolvedPath, OnModelChanged, _token);

        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);

        _ctx = null;
        _token = null;
        _enumType = null;
        _optionsInitialized = false;
    }

    void EnsureOptions(object boxedValue)
    {
        if (_optionsInitialized)
            return;

        if (boxedValue == null)
        {
            Debug.LogWarning(
                $"EnumDropdownBinding: 초기 값이 null 입니다. path='{ResolvedPath}'",
                this);
            return;
        }

        // Nullable<T> 대응
        var t = boxedValue.GetType();
        var enumType = Nullable.GetUnderlyingType(t) ?? t;

        if (!enumType.IsEnum)
        {
            Debug.LogError(
                $"EnumDropdownBinding: path '{ResolvedPath}' 는 Enum 타입이 아닙니다. " +
                $"실제 타입: {enumType.FullName}",
                this);
            return;
        }

        _enumType = enumType;

        dropdown.ClearOptions();
        var names = Enum.GetNames(_enumType);
        dropdown.AddOptions(new List<string>(names));

        _optionsInitialized = true;
    }

    // VM → UI
    void OnModelChanged(object boxed)
    {
        // 첫 값으로 Enum 타입 확정 + 옵션 생성
        EnsureOptions(boxed);
        if (!_optionsInitialized)
            return;

        if (boxed == null)
        {
            // nullable enum에서 null인 경우
            _isUpdatingFromModel = true;
            dropdown.SetValueWithoutNotify(0);
            _isUpdatingFromModel = false;

#if UNITY_EDITOR
            Debug_OnValueUpdated("null", ResolvedPath);
#endif
            return;
        }

        Array values = Enum.GetValues(_enumType);
        int index = Array.IndexOf(values, boxed);

        if (index < 0)
            index = 0;

        _isUpdatingFromModel = true;
        dropdown.SetValueWithoutNotify(index);
        _isUpdatingFromModel = false;

#if UNITY_EDITOR
        Debug_OnValueUpdated(boxed, ResolvedPath);
#endif
    }


    // UI → VM
    void OnDropdownChanged(int index)
    {
        if (_ctx == null || !_optionsInitialized)
            return;

        if (_isUpdatingFromModel)
            return;

        Array values = Enum.GetValues(_enumType);
        if (index < 0 || index >= values.Length)
            return;

        object enumValue = values.GetValue(index);

        _ctx.SetValue(ResolvedPath, enumValue);
    }
}
