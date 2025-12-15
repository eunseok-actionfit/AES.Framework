using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AES.Tools
{
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizationTextBinding : ContextBindingBase
    {
        [SerializeField] TMP_Text tmpText;

        [Header("String Table")]
        [SerializeField] LocalizedStringTable table;

        [Header("Formatting")]
        [SerializeField] bool useFormat;
        [SerializeField, ShowIf(nameof(useFormat))] string format = "{0}";
        [SerializeField, ShowIf(nameof(useFormat))] bool useInvariantCulture = true;

        [Header("Value Converter")]
        [SerializeField] bool useConverter;
        [SerializeField, ShowIf(nameof(useConverter))] ValueConverterSOBase converter;
        [SerializeField, ShowIf(nameof(useConverter))] string converterParameter;

        object _listenerToken;
        IBindingContext _ctx;

        string _currentKey;

        // 비동기 콜백이 꼬여서 오래된 결과가 덮어쓰는 걸 막기 위한 버전 토큰
        int _refreshVersion;

        void OnValidate()
        {
            tmpText ??= GetComponent<TMP_Text>();
        }

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            tmpText ??= GetComponent<TMP_Text>();

            _ctx = context;
            _listenerToken = context.RegisterListener(path, OnKeyChanged);

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

            RefreshText(GetCulture());
        }

        protected override void OnContextUnavailable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

            if (_ctx != null && _listenerToken != null)
                _ctx.RemoveListener(ResolvedPath, _listenerToken);

            _listenerToken = null;
            _ctx = null;
            _currentKey = null;
        }

        void OnKeyChanged(object rawValue)
        {
            var culture = GetCulture();

            object value = rawValue;
            if (useConverter && converter != null)
                value = converter.Convert(value, typeof(object), converterParameter, culture);

            _currentKey = value as string;
            RefreshText(culture);
        }

        void OnLocaleChanged(Locale _)
        {
            RefreshText(GetCulture());
        }

        CultureInfo GetCulture()
            => useInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;

        void RefreshText(CultureInfo culture)
        {
            if (tmpText == null)
                return;

            if (string.IsNullOrWhiteSpace(_currentKey))
            {
                tmpText.text = string.Empty;
                return;
            }

            if (table == null)
            {
                tmpText.text = _currentKey; // fallback
                return;
            }

            int myVersion = ++_refreshVersion;
            
            var handle = table.GetTableAsync();

            if (handle.IsDone)
            {
                if (myVersion != _refreshVersion) return;
                ApplyFromTable(handle.Result, culture);
                return;
            }

            handle.Completed += h =>
            {
                if (this == null || tmpText == null) return;
                if (myVersion != _refreshVersion) return;

                ApplyFromTable(h.Result, culture);
            };
        }

        void ApplyFromTable(StringTable stringTable, CultureInfo culture)
        {
            if (tmpText == null)
                return;

            if (stringTable == null)
            {
                tmpText.text = _currentKey; // fallback
                return;
            }

            var entry = stringTable.GetEntry(_currentKey);
            var localized = entry != null ? entry.GetLocalizedString() : _currentKey;

            tmpText.text = TextFormatHelper.Format(localized, useFormat, format, culture);

#if UNITY_EDITOR
            Debug_OnValueUpdated(tmpText.text, ResolvedPath);
#endif
        }
    }
}
