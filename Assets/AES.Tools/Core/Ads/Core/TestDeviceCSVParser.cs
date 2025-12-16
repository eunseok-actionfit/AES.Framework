using System;
using System.Collections.Generic;
#if !UNITY_EDITOR
using UnityEngine;
#endif

public static class TestDeviceCSVParser
{
    public struct TestDeviceInfo
    {
        public string name;     // 사람이 읽는 이름
        public string adId;     // Advertising ID (UUID)
        public bool adsDisabled;
        public bool isTester;
    }

    /// <summary>
    /// CSV: name,adId
    /// 결과: adId -> info
    /// </summary>
    public static Dictionary<string, TestDeviceInfo> ParseAdIdTable(string csv)
    {
        var dict = new Dictionary<string, TestDeviceInfo>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(csv)) return dict;

        var lines = csv.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("#") || line.StartsWith("//")) continue;

            var idx = line.IndexOf(',');
            if (idx <= 0 || idx >= line.Length - 1) continue;

            var name = line.Substring(0, idx).Trim();
            var adId = line.Substring(idx + 1).Trim();

            if (string.IsNullOrEmpty(adId)) continue;

            dict[adId] = new TestDeviceInfo
            {
                name = name,
                adId = adId,
                adsDisabled = false,
                isTester = true
            };
        }

        return dict;
    }

    /// <summary>
    /// CSV: name,adId
    /// 결과: name -> adId (MAX SetTestDeviceAdvertisingIdentifiers 용)
    /// </summary>
    public static Dictionary<string, string> ParseNameToAdId(string csv)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(csv)) return dict;

        var lines = csv.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("#") || line.StartsWith("//")) continue;

            var idx = line.IndexOf(',');
            if (idx <= 0 || idx >= line.Length - 1) continue;

            var name = line.Substring(0, idx).Trim();
            var adId = line.Substring(idx + 1).Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(adId)) continue;

            dict[name] = adId; // 중복 이름이면 마지막 값으로 덮어씀
        }

        return dict;
    }

    /// <summary>
    /// (옵션) 가능한 경우 현재 디바이스의 Advertising ID를 얻는다.
    /// - 우선 MaxSdk.GetAdvertisingId()를 reflection으로 시도 (없어도 컴파일/런타임 안전)
    /// - iOS는 UnityEngine.iOS.Device.advertisingIdentifier 사용
    /// - 실패하면 SystemInfo.deviceUniqueIdentifier fallback
    /// </summary>
    public static string GetCurrentAdvertisingIdSafe()
    {
#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            return UnityEngine.iOS.Device.advertisingIdentifier;
        }
        catch { }
#endif

        // MaxSdk.GetAdvertisingId() (있을 때만)
        try
        {
            var t = Type.GetType("MaxSdk");
            if (t != null)
            {
                var m = t.GetMethod("GetAdvertisingId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (m != null)
                {
                    var v = m.Invoke(null, null) as string;
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }
        }
        catch { }

#if UNITY_EDITOR
        return "EDITOR_DEVICE";
#else
        return SystemInfo.deviceUniqueIdentifier;
#endif
    }
}
