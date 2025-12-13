using System;
using UnityEngine;


namespace AES.Tools
{
    public class EnumActiveBinding : ContextBindingBase
    {
        [Header("Target Object")]
        public UnityEngine.GameObject target;

        [Header("Enum Compare")]
        public string enumName;      // 실제로 비교에 쓰는 이름
        public bool invert;          // true면 not equal

        // 드롭다운용 캐시
        [SerializeField, HideInInspector] string[] _enumNames;
        [SerializeField, HideInInspector] int _enumIndex;

        IBindingContext _ctx;
        object _token;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _ctx = context;
            _token = context.RegisterListener(path, OnValueChanged);

            if (target == null)
                target = gameObject; // 기본 자기 자신
        }

        protected override void OnContextUnavailable()
        {
            if (_ctx != null && _token != null)
                _ctx.RemoveListener(ResolvedPath, _token);

            _ctx = null;
            _token = null;
        }

        void EnsureEnumNames(Type enumType)
        {
            if (!enumType.IsEnum)
                return;

            var names = Enum.GetNames(enumType);

            // 처음 캐시하거나, enum 타입이 바뀐 경우 갱신
            if (_enumNames == null || _enumNames.Length != names.Length)
            {
                _enumNames = names;

                // enumName 과 매칭되는 인덱스 찾아두기
                _enumIndex = 0;
                if (!string.IsNullOrEmpty(enumName))
                {
                    int idx = Array.IndexOf(_enumNames, enumName);
                    if (idx >= 0)
                        _enumIndex = idx;
                    else
                        enumName = _enumNames.Length > 0 ? _enumNames[0] : enumName;
                }
                else if (_enumNames.Length > 0)
                {
                    enumName = _enumNames[0];
                }
            }
        }

        void OnValueChanged(object value)
        {
            if (value == null)
                return;

            var t = value.GetType();
            if (!t.IsEnum)
            {
                Debug.LogWarning($"EnumActiveBinding: '{ResolvedPath}' 값이 Enum이 아닙니다. (실제 타입: {t.Name})", this);
                return;
            }

            // 드롭다운용 이름 캐시
            EnsureEnumNames(t);

            bool isEqual = false;

            try
            {
                object cmp = Enum.Parse(t, enumName, ignoreCase: true);
                isEqual = Equals(value, cmp);
            }
            catch
            {
                Debug.LogWarning(
                    $"EnumActiveBinding: '{enumName}' 을(를) {t.Name} Enum으로 파싱할 수 없습니다.",
                    this);
            }

            bool active = invert ? !isEqual : isEqual;
            if (target != null)
                target.SetActive(active);

#if UNITY_EDITOR
            // 값 + 실제 경로까지 디버그에 기록
            Debug_OnValueUpdated($"value={value}, active={active}", ResolvedPath);
#endif
        }

    }
}
