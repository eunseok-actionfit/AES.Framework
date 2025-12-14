#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace AES.IAP.Editor.Sheets
{
    public static class SheetDataProcessor_IapJson
    {
        private const string OutputFolderAssetPath = "Assets/Resources/IAP/Generated";

        [Serializable]
        private sealed class JsonRoot
        {
            public string mode;
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

            EnsureOutputFolder();

            foreach (var s in profile.sheets)
                GenerateOne(profile, s);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateOne(SheetAssetProfile_Iap profile, SheetAssetProfile_Iap.SheetInfo sheet)
        {
            if (profile == null || sheet == null) return;

            var fileName = GetOutputFileName(sheet);
            if (string.IsNullOrEmpty(fileName))
                return;

            EnsureOutputFolder();

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

            var rows = GoogleSheetImporter.ParseValues(values); // header=0, skip=1, data=2~
            if (rows == null || rows.Count == 0)
            {
                Debug.LogWarning($"[SheetDataProcessor_IapJson] No rows: {sheet.name}");
                return;
            }

            var root = new JsonRoot
            {
                mode = sheet.mode.ToString(),
                rows = rows
            };

            var json = JsonConvert.SerializeObject(root, Formatting.Indented);
            WriteJson(fileName, json);
        }

        private static string GetOutputFileName(SheetAssetProfile_Iap.SheetInfo sheet)
        {
            var n = (sheet.name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(n))
            {
                Debug.LogError("[SheetDataProcessor_IapJson] sheet.name is empty");
                return null;
            }

            // 숫자만 = gid 실수 방지
            if (long.TryParse(n, out _))
            {
                Debug.LogError($"[SheetDataProcessor_IapJson] sheet.name looks like gid (numeric). Fix name: {n}");
                return null;
            }

            // subfolder 입력은 허용하되 normalize
            n = n.Replace("\\", "/").TrimStart('/');

            // extension 자동 보정
            if (!n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                n += ".json";

            return n;
        }

        private static void EnsureOutputFolder()
        {
            if (Directory.Exists(OutputFolderAssetPath))
                return;

            Directory.CreateDirectory(OutputFolderAssetPath);
            AssetDatabase.Refresh();
        }

        private static void WriteJson(string fileName, string json)
        {
            var fullPath = Path.Combine(OutputFolderAssetPath, fileName).Replace("\\", "/");

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, json, System.Text.Encoding.UTF8);
            Debug.Log($"[SheetDataProcessor_IapJson] Wrote JSON: {fullPath}");
        }
    }
}
#endif
