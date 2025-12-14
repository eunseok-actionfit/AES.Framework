#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace AES.IAP.Editor.Sheets
{
    /// <summary>
    /// IAP-only JSON baker.
    ///  - Downloads via GoogleSheetsPrivateDownloader.DownloadValuesOptimized
    ///  - Parses via GoogleSheetImporter.ParseValues (assumed to skip row#2 comment)
    ///  - Writes: { rows: [ {col:val}, ... ] }
    /// This keeps schema flexible; runtime DB parses strongly-typed DTOs.
    /// </summary>
    public static class SheetDataProcessor_IapJson
    {
        [Serializable]
        private sealed class JsonRoot
        {
            public List<Dictionary<string, string>> rows;
        }

        public static void GenerateAll(SheetAssetProfile_Iap profile)
        {
            if (profile == null)
            {
                Debug.LogError("[SheetDataProcessor_IapJson] profile is null");
                return;
            }
            if (profile.serviceAccountJson == null)
            {
                Debug.LogError("[SheetDataProcessor_IapJson] serviceAccountJson is null");
                return;
            }

            foreach (var s in profile.sheets)
                GenerateOne(profile, s);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateOne(SheetAssetProfile_Iap profile, SheetAssetProfile_Iap.SheetInfo sheet)
        {
            if (profile == null || sheet == null) return;

            IList<IList<object>> values;
            try
            {
                values = GoogleSheetsPrivateDownloader.DownloadValuesOptimized(
                    profile.sheetId,
                    sheet.gid,
                    profile.serviceAccountJson.text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SheetDataProcessor_IapJson] Download failed: {sheet.name} ({sheet.gid})\n{e.Message}");
                return;
            }

            var rows = GoogleSheetImporter.ParseValues(values);
            if (rows == null || rows.Count == 0)
            {
                Debug.LogWarning($"[SheetDataProcessor_IapJson] No rows: {sheet.name}");
                return;
            }

            // IAP 4 modes are all just JSON wrapper writes.
            var root = new JsonRoot { rows = rows };
            var json = JsonUtility.ToJson(root, true);
            WriteJson(sheet, json);
        }

        private static void WriteJson(SheetAssetProfile_Iap.SheetInfo sheet, string json)
        {
            if (sheet.jsonOutputFolder == null)
            {
                Debug.LogError($"[SheetDataProcessor_IapJson] jsonOutputFolder is null: {sheet.name}");
                return;
            }
            if (string.IsNullOrWhiteSpace(sheet.jsonFileName))
            {
                Debug.LogError($"[SheetDataProcessor_IapJson] jsonFileName is empty: {sheet.name}");
                return;
            }

            var folderPath = AssetDatabase.GetAssetPath(sheet.jsonOutputFolder);
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                Debug.LogError($"[SheetDataProcessor_IapJson] Invalid output folder: {folderPath}");
                return;
            }

            var fullPath = Path.Combine(folderPath, sheet.jsonFileName).Replace("\\", "/");
            File.WriteAllText(fullPath, json);
            Debug.Log($"[SheetDataProcessor_IapJson] Wrote JSON: {fullPath}");
        }
    }
}
#endif