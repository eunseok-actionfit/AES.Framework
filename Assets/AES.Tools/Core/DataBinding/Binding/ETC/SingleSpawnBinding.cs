// 파일: SingleSpawnBinding.cs
using System;
using UnityEngine;

namespace AES.Tools
{
    /// <summary>
    /// path에 CatViewModel 같은 단일 객체가 들어오면 prefab을 0 또는 1개 유지.
    /// - 기본은 itemPrefab
    /// - useConverter=true면 (VM -> Prefab) 컨버터 결과로 프리팹 분기
    /// - VM은 같아도 prefab이 바뀌어야 하면 인스턴스를 교체
    /// </summary>
    public sealed class SingleSpawnBinding : ContextBindingBase
    {
        [Header("Root")]
        [SerializeField] private Transform root;

        [Header("Default Prefab")]
        [SerializeField] private MonoContext itemPrefab;

        [Header("Value Converter (VM -> Prefab)")]
        [SerializeField] private bool useConverter = false;
        [SerializeField] private ValueConverterSOBase converter;
        [SerializeField] private string converterParameter;

        private Action<object> _listener;
        private object _token;

        private MonoContext _instance;
        private object _currentVm;
        private MonoContext _currentPrefab;

        void Awake()
        {
            ClearAndCleanRoot();
        }

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token    = context.RegisterListener(path, _listener);
        }

        private void ClearAndCleanRoot()
        {
            _currentVm = null;

            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }

            if (root != null)
            {
                for (int i = root.childCount - 1; i >= 0; i--)
                    Destroy(root.GetChild(i).gameObject);
            }
        }


        protected override void OnContextUnavailable()
        {
            if (_token is IDisposable d) { d.Dispose(); }
            else if (BindingContext != null && _listener != null) { BindingContext.RemoveListener(ResolvedPath, _token); }

            _listener = null;
            _token = null;

            Clear();
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (value == null)
            {
                Clear();
                return;
            }

            var desiredPrefab = ResolvePrefabForVm(value);
            if (desiredPrefab == null)
            {
                // prefab을 못 정하면 안전하게 비움
                Clear();
                return;
            }

            bool vmChanged = !ReferenceEquals(value, _currentVm);
            bool prefabChanged = _currentPrefab != null && _currentPrefab != desiredPrefab;

            // VM이 바뀌었거나, VM은 같은데 prefab이 바뀌어야 하면 교체
            if (vmChanged || prefabChanged || _instance == null)
            {
                ClearInstanceOnly();

                _currentVm = value;
                _currentPrefab = desiredPrefab;

                _instance = Instantiate(desiredPrefab, root);
                _instance.SetViewModel(_currentVm);
                return;
            }

            // 같은 VM + 같은 prefab인데, VM 내부 값만 바뀌는 케이스는
            // VM이 INotifyPropertyChanged류로 업데이트를 흘려주거나
            // 별도 바인딩들이 처리하는 구조로 둔다.
        }

        private MonoContext ResolvePrefabForVm(object vm)
        {
            var prefab = itemPrefab;

            if (useConverter && converter != null)
            {
                var converted = converter.Convert(vm, typeof(MonoContext), converterParameter, null);
                if (converted is MonoContext mc && mc != null)
                    prefab = mc;
            }

            return prefab;
        }

        private void Clear()
        {
            _currentVm = null;

            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }

            if (root != null)
            {
                for (int i = root.childCount - 1; i >= 0; i--)
                    Destroy(root.GetChild(i).gameObject);
            }
        }


        private void ClearInstanceOnly()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}
