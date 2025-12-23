#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AES.IAP.Editor.Sheets
{
    [CreateAssetMenu(menuName = "AES/IAP/Sheet Asset Profile (IAP)", fileName = "SheetAssetProfile_Iap")]
    public sealed class SheetAssetProfile_Iap : ScriptableObject
    {
        [Header("Google Sheet (optional, only used if TSV is not set)")]
        public string sheetId;

        [Tooltip("Service Account JSON(TextAsset). Optional if all sheets use TSV.")]
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
            [Tooltip("JSON file name (without .json). Subfolder allowed, e.g. 'IAP/IAP_Product'.")]
            public string name;

            [Header("TSV (preferred)")]
            [Tooltip("엑셀에서 저장한 TSV(TextAsset). 설정되어 있으면 Google Sheet 대신 이 TSV로 생성합니다.")]
            public TextAsset tsv;

            [Header("Google Sheet (fallback)")]
            [Tooltip("TSV가 없을 때만 사용")]
            public string gid;

            public SheetMode mode;

            [Header("Validation - Unique Key")]
            [Tooltip("중복 체크 키 컬럼들(복합키 지원). 예: ProductKey / ProductKey+Platform. 비우면 중복 체크 안 함.")]
            public List<string> uniqueKeyColumns = new();

            [Header("Validation - EnumDefinition reference")]
            [Tooltip("EnumDefinitionJson에서 EnumName별 EnumValue 집합을 만들어서 참조 검증합니다.")]
            public List<EnumRefRule> enumRefs = new();
        }

        [Serializable]
        public sealed class EnumRefRule
        {
            [Tooltip("검증할 컬럼명 (예: ProductType, Platform, Category, ItemType)")]
            public string columnName;

            [Tooltip("EnumDefinition의 EnumName 값 (예: ProductType, Platform, Category, ItemType)")]
            public string enumName;

            public bool allowEmpty = true;

            [Tooltip("보통 키는 대소문자 구분 추천. 필요하면 true.")]
            public bool ignoreCase = false;
        }
    }
}
#endif
