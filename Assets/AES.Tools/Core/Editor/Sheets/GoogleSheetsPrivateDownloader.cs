#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
public static class GoogleSheetsPrivateDownloader
{
    public static IList<IList<object>> DownloadValuesOptimized(string spreadsheetId, string gid, string serviceAccountJson)
    {
        if (!int.TryParse(gid, out int gidInt))
            throw new Exception($"Invalid gid '{gid}'. gid must be an integer.");

        var service = CreateService(serviceAccountJson);

        string title = ResolveSheetTitle(service, spreadsheetId, gidInt);

        // 1) 헤더만 가져와서 마지막 컬럼 인덱스 찾기 (A1:ZZ1)
        var headerReq = service.Spreadsheets.Values.Get(spreadsheetId, $"{title}!A1:ZZ1");
        var headerVr = headerReq.Execute();

        var headerRow = headerVr?.Values?.FirstOrDefault();
        if (headerRow == null || headerRow.Count == 0)
            return new List<IList<object>>();

        int lastColIndex = LastNonEmptyIndex(headerRow);
        if (lastColIndex < 0)
            return new List<IList<object>>();

        string lastColLetter = ToA1Column(lastColIndex + 1); // 1-based col number

        // 2) 실제 데이터는 A1:lastColLetter (end row 미지정 → API가 사용중인 행만 내려주는 편)
        var dataReq = service.Spreadsheets.Values.Get(spreadsheetId, $"{title}!A1:{lastColLetter}");
        var dataVr = dataReq.Execute();

        return dataVr?.Values ?? new List<IList<object>>();
    }

    private static SheetsService CreateService(string serviceAccountJson)
    {
        // 1) FromJson은 1개 인자만
        var specific = CredentialFactory.FromJson<ServiceAccountCredential>(serviceAccountJson);


        // 2) GoogleCredential로 변환 후 스코프 적용
        var googleCredential = specific
            .ToGoogleCredential()
            .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);

        return new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = googleCredential,
            ApplicationName = "UnitySheetImporter"
        });
    }


    // gid(=SheetId) -> 시트 탭 제목(title) 찾기
    private static string ResolveSheetTitle(SheetsService service, string spreadsheetId, int gid)
    {
        var req = service.Spreadsheets.Get(spreadsheetId);
        req.Fields = "sheets(properties(sheetId,title))";
        Spreadsheet ss = req.Execute();

        var hit = ss.Sheets?.FirstOrDefault(s => s.Properties?.SheetId == gid);
        if (hit?.Properties?.Title == null)
            throw new Exception($"Cannot resolve sheet title for gid={gid}");

        return hit.Properties.Title;
    }

    private static int LastNonEmptyIndex(IList<object> row)
    {
        for (int i = row.Count - 1; i >= 0; i--)
        {
            var s = row[i]?.ToString();
            if (!string.IsNullOrWhiteSpace(s))
                return i;
        }
        return -1;
    }

    // col=1 -> A, 26 -> Z, 27 -> AA ...
    private static string ToA1Column(int col)
    {
        if (col <= 0) throw new ArgumentOutOfRangeException(nameof(col));
        string s = "";
        while (col > 0)
        {
            col--; // 0-based
            s = (char)('A' + (col % 26)) + s;
            col /= 26;
        }
        return s;
    }
}
#endif
