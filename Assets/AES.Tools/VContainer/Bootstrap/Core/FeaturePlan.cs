using System;
using System.Collections.Generic;
using System.Linq;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    public readonly struct FeatureEdge
    {
        public readonly string From;
        public readonly string To;

        public FeatureEdge(string from, string to)
        {
            From = from;
            To = to;
        }
    }

    internal sealed class FeatureEdgeComparer : IEqualityComparer<FeatureEdge>
    {
        public bool Equals(FeatureEdge x, FeatureEdge y)
            => string.Equals(x.From, y.From, StringComparison.Ordinal) &&
               string.Equals(x.To, y.To, StringComparison.Ordinal);

        public int GetHashCode(FeatureEdge obj)
            => HashCode.Combine(obj.From, obj.To);
    }

    public sealed class FeaturePlan
    {
        public readonly struct Node
        {
            public readonly FeatureEntry Entry;
            public readonly AppFeatureSO Feature;

            public Node(FeatureEntry entry, AppFeatureSO feature)
            {
                Entry = entry;
                Feature = feature;
            }
        }

        public IReadOnlyList<Node> Ordered { get; }
        public IReadOnlyList<string> Issues { get; }

        public IReadOnlyList<string> CyclePath { get; } // e.g. A,B,C,A
        public HashSet<FeatureEdge> CycleEdges { get; }  // edges in cycle
        public HashSet<FeatureEdge> MissingDepEdges { get; } // edges that point to missing id

        public bool HasError => Issues.Count > 0;
        public bool HasCycle => CyclePath != null && CyclePath.Count >= 2;

        public FeaturePlan(
            IReadOnlyList<Node> ordered,
            IReadOnlyList<string> issues,
            IReadOnlyList<string> cyclePath = null,
            HashSet<FeatureEdge> cycleEdges = null,
            HashSet<FeatureEdge> missingDepEdges = null)
        {
            Ordered = ordered;
            Issues = issues;
            CyclePath = cyclePath;
            CycleEdges = cycleEdges;
            MissingDepEdges = missingDepEdges;
        }
    }

    public static class FeaturePlanner
    {
        private static readonly FeatureEdgeComparer EdgeComparer = new();

        public static FeaturePlan Build(FeatureProfile profile)
        {
            var issues = new List<string>();
            var entries = profile?.Features ?? Array.Empty<FeatureEntry>();

            // collect
            var nodes = new List<(FeatureEntry Entry, AppFeatureSO Feature, HashSet<string> Deps)>(entries.Count);
            var byId = new Dictionary<string, int>(StringComparer.Ordinal); // id -> index

            foreach (var entry in entries)
            {
                if (entry == null) continue;
                var f = entry.Feature;
                if (!f) continue;

                if (string.IsNullOrWhiteSpace(f.Id))
                {
                    issues.Add($"Feature has empty Id: {f.name}");
                    continue;
                }

                if (byId.ContainsKey(f.Id))
                {
                    issues.Add($"Duplicate feature id: {f.Id}");
                    continue;
                }

                var deps = (f.DependsOn ?? Array.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToHashSet(StringComparer.Ordinal);

                byId.Add(f.Id, nodes.Count);
                nodes.Add((entry, f, deps));
            }

            // missing deps edges
            var missingEdges = new HashSet<FeatureEdge>(EdgeComparer);
            foreach (var n in nodes)
            {
                foreach (var dep in n.Deps)
                {
                    if (!byId.ContainsKey(dep))
                    {
                        issues.Add($"Missing dependency: {n.Feature.Id} depends on {dep}");
                        missingEdges.Add(new FeatureEdge(n.Feature.Id, dep));
                    }
                }
            }

            // topo sort
            var indeg = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var n in nodes) indeg[n.Feature.Id] = 0;

            foreach (var n in nodes)
            {
                foreach (var dep in n.Deps)
                {
                    if (byId.ContainsKey(dep))
                        indeg[n.Feature.Id]++;
                }
            }

            var zero = nodes
                .Where(n => indeg[n.Feature.Id] == 0)
                .OrderBy(n => n.Feature.Order)
                .ThenBy(n => n.Feature.Id, StringComparer.Ordinal)
                .ToList();

            var ordered = new List<FeaturePlan.Node>(nodes.Count);

            while (zero.Count > 0)
            {
                var cur = zero[0];
                zero.RemoveAt(0);

                ordered.Add(new FeaturePlan.Node(cur.Entry, cur.Feature));

                foreach (var n in nodes)
                {
                    if (!n.Deps.Contains(cur.Feature.Id)) continue;

                    indeg[n.Feature.Id]--;
                    if (indeg[n.Feature.Id] == 0)
                    {
                        zero.Add(n);
                        zero = zero
                            .OrderBy(x => x.Feature.Order)
                            .ThenBy(x => x.Feature.Id, StringComparer.Ordinal)
                            .ToList();
                    }
                }
            }

            List<string> cyclePath = null;
            HashSet<FeatureEdge> cycleEdges = null;

            if (ordered.Count != nodes.Count)
            {
                issues.Add("Cycle detected in feature dependencies.");

                // build graph with only present deps
                var graph = new Dictionary<string, string[]>(StringComparer.Ordinal);
                foreach (var n in nodes)
                    graph[n.Feature.Id] = n.Deps.Where(d => byId.ContainsKey(d)).ToArray();

                if (TryFindCyclePath(graph, out var foundPath))
                {
                    cyclePath = foundPath;
                    issues.Add("Cycle path: " + string.Join(" -> ", cyclePath));

                    cycleEdges = new HashSet<FeatureEdge>(EdgeComparer);
                    for (int i = 0; i < cyclePath.Count - 1; i++)
                        cycleEdges.Add(new FeatureEdge(cyclePath[i], cyclePath[i + 1]));
                }

                // fallback deterministic order
                ordered = nodes
                    .OrderBy(n => n.Feature.Order)
                    .ThenBy(n => n.Feature.Id, StringComparer.Ordinal)
                    .Select(n => new FeaturePlan.Node(n.Entry, n.Feature))
                    .ToList();
            }

            return new FeaturePlan(
                ordered,
                issues,
                cyclePath,
                cycleEdges,
                missingEdges.Count > 0 ? missingEdges : null);
        }

        private static bool TryFindCyclePath(
            Dictionary<string, string[]> graph,
            out List<string> cyclePath)
        {
            List<string> foundPath = null;
            cyclePath = null;

            // 0=unvisited, 1=visiting, 2=done
            var state = new Dictionary<string, int>(StringComparer.Ordinal);
            var parent = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var node in graph.Keys)
                state[node] = 0;

            foreach (var start in graph.Keys)
            {
                if (state[start] != 0) continue;
                if (Dfs(start))
                {
                    cyclePath = foundPath;
                    return true;
                }
            }

            return false;

            bool Dfs(string u)
            {
                state[u] = 1;

                if (graph.TryGetValue(u, out var deps))
                {
                    foreach (var v in deps)
                    {
                        if (!graph.ContainsKey(v)) continue;

                        if (state[v] == 0)
                        {
                            parent[v] = u;
                            if (Dfs(v)) return true;
                        }
                        else if (state[v] == 1)
                        {
                            foundPath = BuildPath(from: u, to: v, parent);
                            return true;
                        }
                    }
                }

                state[u] = 2;
                return false;
            }

            static List<string> BuildPath(string from, string to, Dictionary<string, string> parent)
            {
                // to ... from -> to
                var path = new List<string>();
                path.Add(to);

                string cur = from;
                path.Add(cur);

                while (!string.Equals(cur, to, StringComparison.Ordinal))
                {
                    if (!parent.TryGetValue(cur, out var p))
                        break;

                    cur = p;
                    path.Add(cur);
                }

                path.Reverse();
                path.Add(path[0]); // close loop
                return path;
            }
        }
    }
}
