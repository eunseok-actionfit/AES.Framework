using System;
using System.Collections.Generic;
using AES.Tools.Core;
using AES.Tools.SerializedDictionary;
using Sirenix.OdinInspector;
using UnityEngine;


#if ODIN_INSPECTOR


#else
using AYellowpaper.SerializedCollections;
#endif


namespace AES.Tools.Registry
{
#if ODIN_INSPECTOR
    public abstract class UIWindowRegistrySO : SerializedScriptableObject, IUIWindowRegistry 
#else
    public abstract class UIWindowRegistrySO : ScriptableObject, IUIWindowRegistry
#endif
    {
        public abstract bool TryGet(UIWindowKey key, out UIRegistryEntry entry);

        public abstract IEnumerable<KeyValuePair<UIWindowKey, UIRegistryEntry>> GetAll();
    }

    public abstract class UIWindowRegistryBase<TEnum>
        : UIWindowRegistrySO
        where TEnum : Enum

    {
        // Unity에서 직렬화 가능한 Enum → Entry 딕셔너리
        [Serializable]
        public class EntryDictionary
            : UnitySerializedDictionary<TEnum, UIRegistryEntry> { }
        
#if ODIN_INSPECTOR
        [SerializeField]
        private EntryDictionary entries = new();
#else
        [SerializeField]
        private SerializedDictionary<TEnum, UIRegistryEntry> entries = new();
#endif
        

        // 실제 런타임에서 사용하는 키: UIWindowKey → Entry
        private Dictionary<UIWindowKey, UIRegistryEntry> _map;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            if (_map == null)
                _map = new Dictionary<UIWindowKey, UIRegistryEntry>();
            else
                _map.Clear();

            foreach (var kvp in entries)
            {
                var enumId = kvp.Key;
                var uiEntry = kvp.Value;

                if (uiEntry == null)
                    continue;

                var key = UIWindowKey.FromEnum(enumId);
                _map[key] = uiEntry;
            }
        }

        public override bool TryGet(UIWindowKey key, out UIRegistryEntry entry)
        {
            if (_map == null)
                BuildLookup();

            return _map!.TryGetValue(key, out entry);
        }

        // 필요하다면 Enum으로도 직접 조회 가능하게 오버로드 추가
        public bool TryGet(TEnum id, out UIRegistryEntry entry)
        {
            return TryGet(UIWindowKey.FromEnum(id), out entry);
        }

        public override IEnumerable<KeyValuePair<UIWindowKey, UIRegistryEntry>> GetAll()
        {
            if (_map == null)
                BuildLookup();

            return _map;
        }

        // Inspector에서 값 바뀔 때 자동으로 다시 빌드하고 싶으면 선택사항
#if UNITY_EDITOR
        private void OnValidate()
        {
            BuildLookup();
        }
#endif
    }
}