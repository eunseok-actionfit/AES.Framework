using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace AES.Tools
{
    [Serializable]
    public sealed class StorageEntry
    {
        [Tooltip("SaveDataAttribute.Id 와 일치해야 합니다.")]
        public string id;

        [Tooltip("슬롯 ID를 키에 포함할지 여부 (기본: SaveDataAttribute.UseSlot)")]
        public bool? useSlotOverride;

        [Tooltip("백엔드 정책 Override (null 이면 SaveDataAttribute.Backend 사용)")]
        public SaveBackend? backendOverride;
    }

    [CreateAssetMenu(menuName = "SaveSystem/StorageProfile", fileName = "StorageProfile")]
    public sealed class StorageProfile : ScriptableObject
    {
        public List<StorageEntry> entries = new List<StorageEntry>();

        public StorageEntry Find(string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null)
                return null;
            return entries.FirstOrDefault(e => e.id == id);
        }
    }
}