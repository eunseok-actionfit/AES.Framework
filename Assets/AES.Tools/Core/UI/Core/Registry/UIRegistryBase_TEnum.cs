using System;
using System.Collections.Generic;
using AES.Tools.SerializedDictionary;
using AES.Tools.View;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using AYellowpaper.SerializedCollections;
#endif

namespace AES.Tools
{
#if ODIN_INSPECTOR
    public abstract class UIRegistrySO : SerializedScriptableObject, IURegistry 
#else
    public abstract class UIRegistrySO : ScriptableObject, IUIWindowRegistry
#endif
    {
        public abstract bool TryGet(UIWindowKey key, out UIRegistryEntry entry);
        public abstract bool TryGetByViewType(Type viewType, out UIRegistryEntry entry);

        public abstract IEnumerable<KeyValuePair<UIWindowKey, UIRegistryEntry>> GetAll();
    }

    public abstract class UIRegistryBase<TEnum>
        : UIRegistrySO
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
        Dictionary<UIWindowKey, UIRegistryEntry> _map;

        // 뷰 타입 → Entry (PopupManager 에서 타입으로 찾을 때 사용)
        Dictionary<Type, UIRegistryEntry> _typeMap;

        void OnEnable()
        {
            BuildLookup();
        }

        void BuildLookup()
        {
            if (_map == null)
                _map = new Dictionary<UIWindowKey, UIRegistryEntry>();
            else
                _map.Clear();

            if (_typeMap == null)
                _typeMap = new Dictionary<Type, UIRegistryEntry>();
            else
                _typeMap.Clear();

            foreach (var kvp in entries)
            {
                var enumId  = kvp.Key;
                var uiEntry = kvp.Value;

                if (uiEntry == null)
                    continue;

                // 1) Enum → UIWindowKey → Entry
                var key = UIWindowKey.FromEnum(enumId);
                _map[key] = uiEntry;

                // 2) View 타입 → Entry
                //    - Prefab 루트에 UIView 가 있다고 가정
                var prefab = uiEntry.Prefab;
                if (!prefab)
                    continue;

                var view = prefab.GetComponent<UIView>();
                if (!view)
                    continue;

                var viewType = view.GetType();

                // 같은 타입이 여러 번 등록되면 처음 것만 사용 (원하면 나중에 정책 바꿀 수 있음)
                _typeMap.TryAdd(viewType, uiEntry);
            }
        }

        public override bool TryGet(UIWindowKey key, out UIRegistryEntry entry)
        {
            if (_map == null)
                BuildLookup();

            return _map!.TryGetValue(key, out entry);
        }

        // Enum 으로 직접 조회
        public bool TryGet(TEnum id, out UIRegistryEntry entry)
        {
            return TryGet(UIWindowKey.FromEnum(id), out entry);
        }

        // === Type 으로 조회 추가 ===

        public override bool TryGetByViewType(Type viewType, out UIRegistryEntry entry)
        {
            if (viewType == null)
                throw new ArgumentNullException(nameof(viewType));

            if (_typeMap == null)
                BuildLookup();

            return _typeMap!.TryGetValue(viewType, out entry);
        }
        

        public override IEnumerable<KeyValuePair<UIWindowKey, UIRegistryEntry>> GetAll()
        {
            if (_map == null)
                BuildLookup();

            return _map;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            BuildLookup();
        }
#endif
    }
}
