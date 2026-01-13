using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace AES.Tools
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class StringDropdownBinding : ContextBindingBase
    {
        [SerializeField] TMP_Dropdown dropdown;

        [Header("Options")]
        [Tooltip("옵션을 수동으로 지정. 비어 있으면 최초 VM 값(string)으로 옵션 1개 생성")]
        [SerializeField] List<string> predefinedOptions = new();

        [Header("Value Converter")]
        [SerializeField] bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] ValueConverterSOBase converter;
        [SerializeField, ShowIf(nameof(useConverter))] string converterParameter;
        [SerializeField, ShowIf(nameof(useConverter))] bool useInvariantCulture = true;

        IBindingContext _ctx;
        object _token;

        bool _optionsInitialized;
        bool _isUpdatingFromModel;

        void OnValidate()
        {
            dropdown ??= GetComponent<TMP_Dropdown>();
        }

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            dropdown ??= GetComponent<TMP_Dropdown>();

            _ctx = context;

            // VM → UI
            _token = context.RegisterListener(path, OnModelChanged);

            // UI → VM
            dropdown.onValueChanged.AddListener(OnDropdownChanged);

            // 사전 옵션이 있으면 먼저 세팅
            if (predefinedOptions != null && predefinedOptions.Count > 0)
                InitializeOptions(predefinedOptions);
        }

        protected override void OnContextUnavailable()
        {
            if (_ctx != null && _token != null)
                _ctx.RemoveListener(ResolvedPath, _token);

            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnDropdownChanged);

            _ctx = null;
            _token = null;
            _optionsInitialized = false;
        }

        void InitializeOptions(IList<string> options)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            _optionsInitialized = true;
        }

        IFormatProvider GetProvider()
        {
            return useInvariantCulture
                ? CultureInfo.InvariantCulture
                : CultureInfo.CurrentCulture;
        }

        // VM → UI
        void OnModelChanged(object rawValue)
        {
            if (rawValue == null)
                return;

            object value = rawValue;

            if (useConverter && converter != null)
                value = converter.Convert(value, typeof(string), converterParameter, GetProvider());

            if (value is not string displayText)
            {
                Debug.LogError(
                    $"StringDropdownBinding: Converter 결과가 string이 아닙니다. path='{ResolvedPath}'",
                    this);
                return;
            }

            // 옵션 미초기화면: 사전 옵션이 없을 때만, 최초 값으로 옵션 1개 생성
            if (!_optionsInitialized)
                InitializeOptions(new[] { displayText });

            int index = dropdown.options.FindIndex(o => o.text == displayText);
            if (index < 0)
                index = 0;

            _isUpdatingFromModel = true;
            dropdown.SetValueWithoutNotify(index);
            _isUpdatingFromModel = false;

#if UNITY_EDITOR
            Debug_OnValueUpdated(displayText, ResolvedPath);
#endif
        }

        // UI → VM
        void OnDropdownChanged(int index)
        {
            if (_ctx == null || !_optionsInitialized)
                return;

            if (_isUpdatingFromModel)
                return;

            if (index < 0 || index >= dropdown.options.Count)
                return;

            object valueToSet = dropdown.options[index].text;

            if (useConverter && converter != null)
            {
                try
                {
                    valueToSet = converter.ConvertBack(valueToSet, typeof(object), converterParameter, GetProvider());
                }
                catch (System.NotSupportedException)
                {
                    // ConvertBack 미지원이면 그대로 string을 Set
                }
            }

            _ctx.SetValue(ResolvedPath, valueToSet);
        }
    }
}
