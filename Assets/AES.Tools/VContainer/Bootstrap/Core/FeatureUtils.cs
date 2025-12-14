using System;
using System.Collections.Generic;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    public static class FeatureUtils
    {
        public static Dictionary<string, UnityEngine.Object> BuildOverrideMap(IReadOnlyList<OverridePair> pairs)
        {
            if (pairs == null) return null;
            var dict = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
            for (int i = 0; i < pairs.Count; i++)
            {
                var p = pairs[i];
                if (p == null) continue;
                if (string.IsNullOrWhiteSpace(p.key)) continue;
                if (p.value == null) continue;
                dict[p.key] = p.value;
            }
            return dict;
        }
    }
}