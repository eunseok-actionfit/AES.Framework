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

        [Header("Behavior")]
        [Tooltip("테이블 로드 전 표시 정책: true면 키(또는 fallback)를 즉시 표시, false면 빈 문자열")]
        [SerializeField] bool showKeyWhileLoading = true;

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

        // 비동기 콜백 꼬임 방지용 버전 토큰
        int _refreshVersion;

        // 테이블 캐시
        StringTable _cachedTable;
        AsyncOperationHandle<StringTable>? _tableHandle;

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

            // 시작 시 테이블 워밍업 + 현재 키(없으면 빈값) 반영
            PreloadTableAndRefresh(GetCulture());
        }

        protected override void OnContextUnavailable()
        {
            Cleanup();
        }

        void Cleanup()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

            if (_ctx != null && _listenerToken != null)
                _ctx.RemoveListener(ResolvedPath, _listenerToken);

            _listenerToken = null;
            _ctx = null;
            _currentKey = null;

            _cachedTable = null;

            // Addressables/ResourceManager 핸들 해제
            ReleaseTableHandleIfNeeded();
        }

        void ReleaseTableHandleIfNeeded()
        {
            if (_tableHandle.HasValue)
            {
                var h = _tableHandle.Value;
                if (h.IsValid())
                {
                    // 이미 Release된 핸들을 다시 Release하면 에러날 수 있어 IsValid 체크
                    // ResourceManager를 통해 로드된 테이블 핸들이므로 Release로 반환
                    AddressablesSafeRelease(h);
                }
                _tableHandle = null;
            }
        }

        // Addressables 패키지를 직접 참조 안 하는 프로젝트도 있어 ResourceManager 기반으로 안전 처리
        static void AddressablesSafeRelease<T>(AsyncOperationHandle<T> handle)
        {
            try
            {
                // UnityEngine.AddressableAssets.Addressables.Release(handle) 를 쓰는 프로젝트면 이걸로 교체 가능
                // 여기선 ResourceManager로 Release만 호출
                if (handle.IsValid())
                    handle.Release();
            }
            catch
            {
                // ignore
            }
        }

        void OnKeyChanged(object rawValue)
        {
            var culture = GetCulture();

            object value = rawValue;
            if (useConverter && converter != null)
                value = converter.Convert(value, typeof(object), converterParameter, culture);

            _currentKey = value as string;

            // 테이블은 캐시되어 있으면 즉시 적용, 없으면 로드 진행 + 로딩 표시
            RefreshText(culture);
            EnsureTableLoaded(culture);
        }

        void OnLocaleChanged(Locale _)
        {
            var culture = GetCulture();

            // 로케일이 바뀌면 테이블 인스턴스가 바뀔 수 있으니 캐시 무효화 후 재로드
            _cachedTable = null;
            ReleaseTableHandleIfNeeded();

            // 로딩 표시를 먼저 갱신하고, 테이블 로드 후 다시 한번 적용
            RefreshText(culture);
            PreloadTableAndRefresh(culture);
        }

        CultureInfo GetCulture()
            => useInvariantCulture ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture;

        void PreloadTableAndRefresh(CultureInfo culture)
        {
            EnsureTableLoaded(culture, refreshAfterLoaded: true);
            RefreshText(culture);
        }

        void EnsureTableLoaded(CultureInfo culture, bool refreshAfterLoaded = false)
        {
            if (table == null)
                return;

            // 이미 캐시가 있으면 끝
            if (_cachedTable != null)
                return;

            // 이미 로드 중/완료 핸들이 있으면 재사용
            if (_tableHandle.HasValue)
            {
                var existing = _tableHandle.Value;
                if (existing.IsValid())
                {
                    if (existing.IsDone)
                    {
                        _cachedTable = existing.Result;
                        if (refreshAfterLoaded) RefreshText(culture);
                    }
                    return;
                }
                _tableHandle = null;
            }

            int myVersion = ++_refreshVersion;

            var handle = table.GetTableAsync();
            _tableHandle = handle;

            if (handle.IsDone)
            {
                if (myVersion != _refreshVersion) return;
                _cachedTable = handle.Result;
                if (refreshAfterLoaded) RefreshText(culture);
                return;
            }

            handle.Completed += h =>
            {
                if (this == null || tmpText == null) return;
                if (myVersion != _refreshVersion) return;

                _cachedTable = h.Result;
                if (refreshAfterLoaded) RefreshText(culture);
                else RefreshText(culture); // 키가 이미 있으면 번역으로 교체
            };
        }

        void RefreshText(CultureInfo culture)
        {
            if (tmpText == null)
                return;

            if (string.IsNullOrWhiteSpace(_currentKey))
            {
                tmpText.text = string.Empty;
                return;
            }

            // 테이블이 없으면 fallback
            if (table == null)
            {
                tmpText.text = _currentKey;
                return;
            }

            // 테이블이 아직 로드 전이면 정책에 따라 즉시 표시
            if (_cachedTable == null)
            {
                tmpText.text = showKeyWhileLoading ? _currentKey : string.Empty;
                return;
            }

            ApplyFromTable(_cachedTable, culture);
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
