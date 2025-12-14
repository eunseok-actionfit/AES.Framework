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
        }

        [Serializable]
        public sealed class SheetInfo
        {
            public string name;
            public string gid;
            public SheetMode mode;

            [Header("JSON Output")]
            [Tooltip("Output folder under Assets. Recommended: Assets/Resources/IAP/Generated")]
            public UnityEngine.Object jsonOutputFolder;

            [Tooltip("File name. ex: IapProduct.json")]
            public string jsonFileName;
        }
    }
}
#endif