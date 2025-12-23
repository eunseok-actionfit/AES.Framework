#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // ===== Menu Buttons =====
        [MenuItem("AES/IAP/Sheets/Generate All (Profile)")]
        private static void MenuGenerateAll()
        {
            var profile = FindProfile();
            if (profile == null) return;
            GenerateAll(profile);
        }

        [MenuItem("AES/IAP/Sheets/Generate From Selected TSV")]
        private static void MenuGenerateFromSelectedTsv()
        {
            var profile = FindProfile();
            if (profile == null) return;

            var selected = Selection.objects.OfType<TextAsset>().ToArray();
            if (selected.Length == 0)
            {
                Debug.LogWarning("[SheetDataProcessor_IapJson] Select one or more TSV(TextAsset).");
                return;
            }

            var enumLookup = SheetValidation_Iap.BuildEnumLookupFromProfile(profile, out var enumErrors);
            if (enumErrors.Count > 0)
            {
                Debug.LogError("[SheetDataProcessor_IapJson] EnumDefinition build failed:\n- " + string.Join("\n- ", enumErrors));
                return;
            }

            EnsureOutputFolder();

            int ok = 0, fail = 0;
            foreach (var ta in selected)
            {
                var sheet = profile.sheets?.FirstOrDefault(s => s != null && s.tsv == ta);
                if (sheet == null)
                {
                    Debug.LogWarning($"[SheetDataProcessor_IapJson] Selected TSV not found in profile.sheets: {ta.name}");
                    fail++;
                    continue;
                }

                if (GenerateOne(profile, sheet, enumLookup)) ok++; else fail++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SheetDataProcessor_IapJson] Generate From Selected TSV done. ok={ok}, fail={fail}");
        }

        private static SheetAssetProfile_Iap FindProfile()
        {
            var guid = AssetDatabase.FindAssets("t:SheetAssetProfile_Iap").FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError("[SheetDataProcessor_IapJson] SheetAssetProfile_Iap not found.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<SheetAssetProfile_Iap>(AssetDatabase.GUIDToAssetPath(guid));
        }

        // ===== Main =====
        public static void GenerateAll(SheetAssetProfile_Iap profile)
        {
            if (profile == null)
            {
                Debug.LogError("[SheetDataProcessor_IapJson] profile is null");
                return;
            }

            if (profile.sheets == null || profile.sheets.Count == 0)
            {
                Debug.LogWarning("[SheetDataProcessor_IapJson] profile.sheets is empty");
                return;
            }

            // TSV 없는 시트가 하나라도 있으면 Google 설정 필요
            bool needsGoogle = profile.sheets.Any(s => s != null && s.tsv == null);
            if (needsGoogle)
            {
                if (string.IsNullOrWhiteSpace(profile.sheetId))
                {
                    Debug.LogError("[SheetDataProcessor_IapJson] sheetId is empty (required for non-TSV sheets)");
                    return;
                }

                if (profile.serviceAccountJson == null)
                {
                    Debug.LogError("[SheetDataProcessor_IapJson] serviceAccountJson is null (required for non-TSV sheets)");
                    return;
                }
            }

            // EnumDefinition lookup 준비
            var enumLookup = SheetValidation_Iap.BuildEnumLookupFromProfile(profile, out var enumErrors);
            if (enumErrors.Count > 0)
            {
                Debug.LogError("[SheetDataProcessor_IapJson] EnumDefinition build failed:\n- " + string.Join("\n- ", enumErrors));
                return;
            }

            EnsureOutputFolder();

            int ok = 0, fail = 0;
            foreach (var s in profile.sheets)
            {
                if (s == null) continue;
                if (GenerateOne(profile, s, enumLookup)) ok++; else fail++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SheetDataProcessor_IapJson] GenerateAll done. ok={ok}, fail={fail}");
        }

        // return true on success
        public static bool GenerateOne(
            SheetAssetProfile_Iap profile,
            SheetAssetProfile_Iap.SheetInfo sheet,
            IReadOnlyDictionary<string, HashSet<string>> enumLookup = null)
        {
            if (profile == null || sheet == null) return false;

            var fileName = GetOutputFileName(sheet);
            if (string.IsNullOrEmpty(fileName))
                return false;

            EnsureOutputFolder();

            List<Dictionary<string, string>> rows = null;

            // 1) TSV 우선
            if (sheet.tsv != null)
            {
                rows = GoogleSheetImporter.ParseTSV(sheet.tsv.text);
            }
            else
            {
                // 2) Google Sheets fallback
                if (string.IsNullOrWhiteSpace(profile.sheetId) || profile.serviceAccountJson == null)
                {
                    Debug.LogError($"[SheetDataProcessor_IapJson] Google path requires sheetId and serviceAccountJson: {sheet.name}");
                    return false;
                }

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
                    return false;
                }

                rows = GoogleSheetImporter.ParseValues(values);
            }

            if (rows == null || rows.Count == 0)
            {
                Debug.LogWarning($"[SheetDataProcessor_IapJson] No rows: {sheet.name}");
                return false;
            }

            // 3) Validate (EnumDefinitionJson은 lookup이 없어도 통과 가능)
            var errors = SheetValidation_Iap.ValidateSheet(sheet, rows, enumLookup);
            if (errors.Count > 0)
            {
                Debug.LogError($"[SheetDataProcessor_IapJson] Validation failed: {sheet.name}\n- " + string.Join("\n- ", errors));
                return false;
            }

            var root = new JsonRoot
            {
                mode = sheet.mode.ToString(),
                rows = rows
            };

            var json = JsonConvert.SerializeObject(root, Formatting.Indented);
            WriteJson(fileName, json);
            return true;
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
