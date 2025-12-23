#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace AES.IAP.Editor.Sheets
{
    public static class SheetValidation_Iap
    {
        // EnumDefinitionJson의 rows에서 lookup 생성:
        // EnumName -> { EnumValue }
        public static Dictionary<string, HashSet<string>> BuildEnumLookupFromProfile(
            SheetAssetProfile_Iap profile,
            out List<string> errors)
        {
            errors = new List<string>();
            var lookup = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            if (profile == null || profile.sheets == null)
                return lookup;

            var enumSheet = profile.sheets.FirstOrDefault(s => s != null && s.mode == SheetAssetProfile_Iap.SheetMode.EnumDefinitionJson);
            if (enumSheet == null)
            {
                // EnumDefinition이 없으면 enumRefs 검증은 경고로 처리(필요시 errors로 바꿔도 됨)
                return lookup;
            }

            // enum sheet rows parse (TSV 우선)
            List<Dictionary<string, string>> rows = null;

            if (enumSheet.tsv != null)
            {
                rows = GoogleSheetImporter.ParseTSV(enumSheet.tsv.text);
            }
            else
            {
                // Google fallback은 GenerateAll에서 이미 체크한다고 가정
                errors.Add("EnumDefinition sheet has no TSV. Recommend providing TSV for EnumDefinitionJson.");
                return lookup;
            }

            if (rows == null || rows.Count == 0)
            {
                errors.Add("EnumDefinition rows are empty.");
                return lookup;
            }

            // required columns
            const string COL_NAME = "EnumName";
            const string COL_VALUE = "EnumValue";
            const string COL_ACTIVE = "IsActive";

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r == null) continue;

                r.TryGetValue(COL_NAME, out var enumName);
                r.TryGetValue(COL_VALUE, out var enumValue);
                r.TryGetValue(COL_ACTIVE, out var isActive);

                enumName = (enumName ?? "").Trim();
                enumValue = (enumValue ?? "").Trim();
                isActive = (isActive ?? "").Trim();

                if (string.IsNullOrEmpty(enumName) || string.IsNullOrEmpty(enumValue))
                {
                    errors.Add($"EnumDefinition Row#{i}: EnumName/EnumValue is empty.");
                    continue;
                }

                // IsActive가 있으면 TRUE만 포함(없으면 모두 포함)
                if (!string.IsNullOrEmpty(isActive) && !IsTrue(isActive))
                    continue;

                if (!lookup.TryGetValue(enumName, out var set))
                {
                    set = new HashSet<string>(StringComparer.Ordinal);
                    lookup[enumName] = set;
                }

                if (!set.Add(enumValue))
                {
                    errors.Add($"EnumDefinition duplicate: {enumName}.{enumValue} at Row#{i}");
                }
            }

            return lookup;
        }

        public static List<string> ValidateSheet(
            SheetAssetProfile_Iap.SheetInfo sheet,
            List<Dictionary<string, string>> rows,
            IReadOnlyDictionary<string, HashSet<string>> enumLookup)
        {
            var errors = new List<string>();
            if (sheet == null) { errors.Add("sheet is null"); return errors; }
            if (rows == null) { errors.Add("rows is null"); return errors; }

            ValidateUniqueKey(sheet, rows, errors);
            ValidateEnumRefs(sheet, rows, enumLookup, errors);

            return errors;
        }

        private static void ValidateUniqueKey(
            SheetAssetProfile_Iap.SheetInfo sheet,
            List<Dictionary<string, string>> rows,
            List<string> errors)
        {
            if (sheet.uniqueKeyColumns == null || sheet.uniqueKeyColumns.Count == 0)
                return;

            var cols = sheet.uniqueKeyColumns
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToArray();

            if (cols.Length == 0) return;

            var seen = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                if (r == null) continue;

                // 복합키 문자열 생성
                bool anyEmpty = false;
                var parts = new string[cols.Length];

                for (int c = 0; c < cols.Length; c++)
                {
                    r.TryGetValue(cols[c], out var v);
                    v = (v ?? "").Trim();
                    parts[c] = v;
                    if (string.IsNullOrEmpty(v)) anyEmpty = true;
                }

                if (anyEmpty)
                {
                    errors.Add($"{sheet.name} Row#{i}: unique key has empty column(s): [{string.Join(", ", cols)}]");
                    continue;
                }

                var key = string.Join("|", parts);
                if (seen.TryGetValue(key, out var prev))
                    errors.Add($"{sheet.name}: duplicate key '{key}' at Row#{prev} and Row#{i}");
                else
                    seen[key] = i;
            }
        }

        private static void ValidateEnumRefs(
            SheetAssetProfile_Iap.SheetInfo sheet,
            List<Dictionary<string, string>> rows,
            IReadOnlyDictionary<string, HashSet<string>> enumLookup,
            List<string> errors)
        {
            if (sheet.enumRefs == null || sheet.enumRefs.Count == 0)
                return;

            // EnumDefinition이 없으면 enumRefs 검증은 실패로 처리(원하면 경고로 바꿀 수 있음)
            if (enumLookup == null || enumLookup.Count == 0)
            {
                errors.Add($"{sheet.name}: enumRefs set but EnumDefinition lookup is empty/missing.");
                return;
            }

            foreach (var rule in sheet.enumRefs)
            {
                if (rule == null) continue;

                var col = (rule.columnName ?? "").Trim();
                var enumName = (rule.enumName ?? "").Trim();
                if (string.IsNullOrEmpty(col) || string.IsNullOrEmpty(enumName)) continue;

                if (!enumLookup.TryGetValue(enumName, out var allowed))
                {
                    errors.Add($"{sheet.name}: EnumName '{enumName}' not found in EnumDefinition.");
                    continue;
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    if (r == null) continue;

                    r.TryGetValue(col, out var raw);
                    raw = (raw ?? "").Trim();

                    if (string.IsNullOrEmpty(raw))
                    {
                        if (!rule.allowEmpty)
                            errors.Add($"{sheet.name} Row#{i}: enum column '{col}' is empty");
                        continue;
                    }

                    bool ok = rule.ignoreCase
                        ? allowed.Any(v => string.Equals(v, raw, StringComparison.OrdinalIgnoreCase))
                        : allowed.Contains(raw);

                    if (!ok)
                        errors.Add($"{sheet.name} Row#{i}: invalid enum '{col}'='{raw}' (EnumName={enumName})");
                }
            }
        }

        private static bool IsTrue(string s)
        {
            s = (s ?? "").Trim();
            return s.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                   s.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   s.Equals("YES", StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
