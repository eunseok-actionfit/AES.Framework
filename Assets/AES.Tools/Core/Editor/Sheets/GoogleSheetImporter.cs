using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GoogleSheetImporter
{
    // TSV 파서(유지)
    public static List<Dictionary<string, string>> ParseTSV(string raw)
    {
        string[] lines = raw.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        if (lines.Length < 2)
        {
            Debug.LogWarning("TSV 데이터가 부족합니다.");
            return new List<Dictionary<string, string>>();
        }

        string[] headers = lines[0]
            .Split('\t')
            .Select(h => h.Trim().Replace("\uFEFF", ""))
            .ToArray();

        List<Dictionary<string, string>> rows = new();

        // 규칙: 0=헤더, 1줄 스킵, 2줄부터 데이터
        for (int i = 2; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split('\t');
            if (cols.All(string.IsNullOrWhiteSpace))
                continue;

            Dictionary<string, string> row = new();
            bool isCommentedRow = false;

            for (int j = 0; j < headers.Length; j++)
            {
                string key = headers[j];
                string val = (j < cols.Length ? cols[j] : "").Trim();

                if (val.StartsWith("@"))
                    isCommentedRow = true;

                row[key] = val;
            }

            if (!isCommentedRow)
                rows.Add(row);
        }

        return rows;
    }

#if UNITY_EDITOR
    // Google Sheets Values(API) 결과 파싱
    // 규칙 유지: 0=헤더, 1줄 스킵, 2줄부터 데이터, '@'로 시작하는 셀이 있으면 행 스킵
    public static List<Dictionary<string, string>> ParseValues(IList<IList<object>> values)
    {
        if (values == null || values.Count < 2)
        {
            Debug.LogWarning("Sheet Values 데이터가 부족합니다.");
            return new List<Dictionary<string, string>>();
        }

        var headers = values[0]
            .Select(v => (v?.ToString() ?? "").Trim().Replace("\uFEFF", ""))
            .ToList();

        if (headers.Count == 0)
        {
            Debug.LogWarning("헤더가 비어있습니다.");
            return new List<Dictionary<string, string>>();
        }

        var rows = new List<Dictionary<string, string>>();

        for (int i = 2; i < values.Count; i++)
        {
            var line = values[i];
            if (line == null || line.Count == 0) continue;

            bool allEmpty = true;
            for (int c = 0; c < line.Count; c++)
            {
                if (!string.IsNullOrWhiteSpace(line[c]?.ToString()))
                {
                    allEmpty = false;
                    break;
                }
            }
            if (allEmpty) continue;

            var row = new Dictionary<string, string>();
            bool isCommentedRow = false;

            int colCount = Mathf.Min(headers.Count, line.Count);
            for (int j = 0; j < colCount; j++)
            {
                string key = headers[j];
                string val = (line[j]?.ToString() ?? "").Trim();

                if (val.StartsWith("@"))
                    isCommentedRow = true;

                row[key] = val;
            }

            // 누락 컬럼은 빈 문자열로 채움
            for (int j = colCount; j < headers.Count; j++)
                row[headers[j]] = "";

            if (!isCommentedRow)
                rows.Add(row);
        }

        return rows;
    }
#endif
    
}
