#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AES.IAP.Editor.Sheets
{
    public sealed class SheetTsvAutoReimport_Iap : AssetPostprocessor
    {
        private static bool s_Running;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (s_Running) return;

            // 엑셀 탭분리 텍스트는 .txt가 가장 안정적. .tsv도 허용.
            var changed = importedAssets
                .Where(p => p.EndsWith(".tsv") || p.EndsWith(".txt"))
                .ToArray();

            if (changed.Length == 0) return;

            var profile = FindProfile();
            if (profile == null || profile.sheets == null || profile.sheets.Count == 0) return;

            var hitSheets = new List<SheetAssetProfile_Iap.SheetInfo>();

            foreach (var path in changed)
            {
                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (ta == null) continue;

                var hit = profile.sheets.FirstOrDefault(s => s != null && s.tsv == ta);
                if (hit != null) hitSheets.Add(hit);
            }

            if (hitSheets.Count == 0) return;

            try
            {
                s_Running = true;

                // lookup 포함 전체 생성(가장 단순/안전)
                SheetDataProcessor_IapJson.GenerateAll(profile);
            }
            finally
            {
                s_Running = false;
            }
        }

        private static SheetAssetProfile_Iap FindProfile()
        {
            var guid = AssetDatabase.FindAssets("t:SheetAssetProfile_Iap").FirstOrDefault();
            if (string.IsNullOrEmpty(guid)) return null;
            return AssetDatabase.LoadAssetAtPath<SheetAssetProfile_Iap>(AssetDatabase.GUIDToAssetPath(guid));
        }
    }
}
#endif