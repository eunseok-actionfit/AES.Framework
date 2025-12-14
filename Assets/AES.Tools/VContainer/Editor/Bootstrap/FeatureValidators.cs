using System;
using System.Collections.Generic;
using System.Linq;


namespace AES.Tools.VContainer.Bootstrap.Framework.Editor
{
    public static class FeatureValidators
    {
        public static List<string> ValidateProfile(FeatureProfile profile)
        {
            if (profile == null) return new List<string>();
            var plan = FeaturePlanner.Build(profile);
            return plan.Issues.ToList();
        }

        private static bool HasCycle(IReadOnlyList<FeatureEntry> entries)
        {
            var graph = new Dictionary<string, string[]>(StringComparer.Ordinal);

            foreach (var e in entries)
            {
                var f = e?.Feature;
                if (!f) continue;

                graph[f.Id] = (f.DependsOn ?? Array.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
            }

            var visiting = new HashSet<string>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);

            bool Dfs(string id)
            {
                if (visited.Contains(id)) return false;
                if (!visiting.Add(id)) return true;

                if (graph.TryGetValue(id, out var deps))
                {
                    foreach (var d in deps)
                    {
                        if (!graph.ContainsKey(d)) continue;
                        if (Dfs(d)) return true;
                    }
                }

                visiting.Remove(id);
                visited.Add(id);
                return false;
            }

            foreach (var k in graph.Keys)
                if (Dfs(k)) return true;

            return false;
        }
    }
}
