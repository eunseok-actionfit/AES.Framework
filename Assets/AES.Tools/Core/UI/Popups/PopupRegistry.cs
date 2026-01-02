using System;
using System.Collections.Generic;
using UnityEngine;


namespace AES.Tools.UI.Popups
{
    [CreateAssetMenu(menuName = "UI/Popups/Popup Registry")]
    public sealed class PopupRegistry : ScriptableObject
    {
        [Header("Popup 프리팹들 (루트 또는 자식에 PopupViewBase 파생 포함)")]
        [SerializeField] private GameObject[] prefabs;

        private Dictionary<Type, GameObject> _map;
        
        private void OnValidate()
        {
            Build();
        }

        private void OnEnable()
        {
            Build();
        }

        private void Build()
        {
            _map = new Dictionary<Type, GameObject>(prefabs?.Length ?? 16);

            if (prefabs == null)
                return;

            foreach (var prefab in prefabs)
            {
                if (prefab == null)
                    continue;

                var views = prefab.GetComponentsInChildren<PopupViewBase>(true);
                if (views == null || views.Length == 0)
                {
                    Debug.LogError(
                        $"[PopupRegistry] '{prefab.name}' 에 PopupViewBase 파생 컴포넌트가 없습니다.",
                        this);
                    continue;
                }

                // 하나의 프리팹에 여러 View 타입이 있어도 모두 등록
                foreach (var view in views)
                {
                    var viewType = view.GetType();
                    _map[viewType] = prefab;
                }
            }
        }

        public GameObject Resolve(Type viewType)
        {
            if (_map == null)
                Build();

            return _map.TryGetValue(viewType, out var prefab)
                ? prefab
                : null;
        }
    }
}