#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AES.IAP.Editor.Sheets
{
    [CreateAssetMenu(menuName = "AES/IAP/Sheet Asset Profile (IAP)", fileName = "SheetAssetProfile_Iap")]
    public sealed class SheetAssetProfile_Iap : ScriptableObject
    {
        [Header("Google Sheet")]
        public string sheetId;

        [Tooltip("Service Account JSON(TextAsset). Place under an Editor-only folder.")]
        public TextAsset serviceAccountJson;

        public List<SheetInfo> sheets = new();

        public enum SheetMode
        {
            EnumDefinitionJson,
            IapProductJson,
            IapStoreProductJson,
            IapBundleContentJson,
            EconomyValueJson,
            IapLimitJson,
        }

        [Serializable]
        public sealed class SheetInfo
        {
            [Tooltip("Sheet display name AND json file name (without .json)")]
            public string name;

            public string gid;
            public SheetMode mode;
        }
    }
}
#endif