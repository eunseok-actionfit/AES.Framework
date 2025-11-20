// Assets/Framework/Systems/UI/Core/Registry/UIRegistryEntry.cs
using System;
using AES.Tools.Core;
using AES.Tools.Core.UIController;
using AES.Tools.Core.UIRoot;
using UnityEngine;


namespace AES.Tools.Registry
{
    /// Show 전에 필요한 정보만 보관
    [Serializable]
    public sealed class UIRegistryEntry
    {
        // 소스: Prefab 하나만 입력. Addressables 등록 시 자동 인식
        [SerializeField] private GameObject prefab;
        [SerializeField] private string addressGuid; // Addressables GUID(등록 안 되어 있으면 빈 문자열)

        // 배치
        public UIRootRole Scope = UIRootRole.Local;
        public UILayerKind  Kind;

        // 수명
        public UIInstancePolicy InstancePolicy = UIInstancePolicy.Singleton;
        public UIConcurrency    Concurrency    = UIConcurrency.Queue;
        public UIExclusiveGroup ExclusiveGroup = UIExclusiveGroup.None;


        // 최적화
        public bool UsePool = false;
        [Tooltip("풀의 최대 보유 수")]
        [Min(1)]
        public int Capacity = 1;

        [Tooltip("초기 생성 수량 (0~Capacity)")]
        [Min(0)]
        public int WarmUp = 0;

        [Tooltip("Hide 후 반환까지의 지연 시간(초)")]
        [Min(0)]
        public float ReturnDelay = 0f;
        [NonSerialized] public Type DataContractType; // 모델 타입(검증용)
        
        // 런타임 참조용 읽기 API
        public GameObject Prefab        => prefab;
        public string     AddressGuid   => addressGuid;
        public bool       IsAddressable => !string.IsNullOrEmpty(addressGuid);
        
    }
}