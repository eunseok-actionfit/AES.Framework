using System;
using System.Collections.Generic;


public static class TestDeviceCSVParser
{
    public struct TestDeviceInfo
    {
        public string name;
        public string deviceId;
        public bool adsDisabled;
        public bool isTester;
    }

    public static Dictionary<string, TestDeviceInfo> Parse(string csv)
    {
        var dict = new Dictionary<string, TestDeviceInfo>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(csv))
            return dict;

        var lines = csv.Split('\n');
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line))
                continue;
            if (line.StartsWith("#"))
                continue;

            var cols = line.Split(',');
            if (cols.Length < 2)
                continue;

            var name     = cols[0].Trim();
            var deviceId = cols[1].Trim();

            if (string.IsNullOrEmpty(deviceId))
                continue;

            dict[deviceId] = new TestDeviceInfo
            {
                name        = name,
                deviceId    = deviceId,
                adsDisabled = false, 
                isTester    = true
            };
        }

        return dict;
    }

}