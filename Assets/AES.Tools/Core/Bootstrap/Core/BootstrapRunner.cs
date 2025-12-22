using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Debug = UnityEngine.Debug;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    internal static class BootstrapRunnerCacheReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Reset() => BootstrapRunner.ClearCache();
    }

    public static class BootstrapRunner
    {
        public readonly struct Result
        {
            public readonly IReadOnlyList<FeatureRun> Runs;
            public readonly bool Success;

            public Result(IReadOnlyList<FeatureRun> runs, bool success)
            {
                Runs = runs;
                Success = success;
            }
        }

        public readonly struct FeatureRun
        {
            public readonly string Id;
            public readonly bool Enabled;
            public readonly long InstallMs;
            public readonly long InitMs;
            public readonly Exception Exception;

            public FeatureRun(string id, bool enabled, long installMs, long initMs, Exception ex)
            {
                Id = id;
                Enabled = enabled;
                InstallMs = installMs;
                InitMs = initMs;
                Exception = ex;
            }
        }

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            public readonly int GraphId;
            public readonly string Profile;
            public CacheKey(int graphId, string profile)
            {
                GraphId = graphId;
                Profile = profile ?? "";
            }
            public bool Equals(CacheKey other) => GraphId == other.GraphId && string.Equals(Profile, other.Profile, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is CacheKey k && Equals(k);
            public override int GetHashCode() => HashCode.Combine(GraphId, Profile);
        }

        private static readonly Dictionary<CacheKey, FeaturePlan> PlanCache = new();
        public static void ClearCache() => PlanCache.Clear();

        private static bool TryGetProfile(BootstrapGraph graph, string profile, out FeatureProfile p)
        {
            if (!graph.TryGetProfile(profile, out p))
                graph.TryGetProfile(graph.DefaultProfile, out p);
            return p != null;
        }

        private static FeaturePlan GetPlan(BootstrapGraph graph, string profile)
        {
            var key = new CacheKey(graph.GetInstanceID(), profile);

            if (PlanCache.TryGetValue(key, out var plan) && plan != null)
                return plan;

            if (!TryGetProfile(graph, profile, out var p))
                return new FeaturePlan(Array.Empty<FeaturePlan.Node>(), Array.Empty<string>());

            plan = FeaturePlanner.Build(p);
            PlanCache[key] = plan;
            return plan;
        }

        public static Result InstallAll(
            BootstrapGraph graph,
            string profile,
            IContainerBuilder builder,
            RuntimePlatform platform,
            bool isEditor)
        {
            var runs = new List<FeatureRun>(64);
            bool ok = true;

            if (!graph) return new Result(runs, true);

            var plan = GetPlan(graph, profile);

            if (plan.Issues.Count > 0)
            {
                ok = false;
                foreach (var s in plan.Issues) Debug.LogError($"[BootstrapGraph] {s}");
            }

            var caps = BuildCapabilities(plan, profile, platform, isEditor);

            foreach (var n in plan.Ordered)
            {
                var entry = n.Entry;
                var feature = n.Feature;

                var ov = FeatureUtils.BuildOverrideMap(entry.Overrides);
                var ctx = new FeatureContext(profile, platform, isEditor, ov, caps, progress: null);

                bool enabled = entry.Enabled && feature.IsEnabled(in ctx);

                long ms = 0;
                Exception ex = null;

                if (enabled)
                {
                    var sw = Stopwatch.StartNew();
                    try { feature.Install(builder, in ctx); }
                    catch (Exception e) { ex = e; ok = false; Debug.LogException(e); }
                    sw.Stop();
                    ms = sw.ElapsedMilliseconds;
                }

                runs.Add(new FeatureRun(feature.Id, enabled, ms, 0, ex));
            }

            return new Result(runs, ok);
        }

        public async static UniTask<Result> InitializeAllAsync(
            BootstrapGraph graph,
            string profile,
            LifetimeScope root,
            RuntimePlatform platform,
            bool isEditor,
            IProgress<BootstrapProgress> progress = null)
        {
            var runs = new List<FeatureRun>(64);
            bool ok = true;

            if (!graph) return new Result(runs, true);

            var plan = GetPlan(graph, profile);

            if (plan.Issues.Count > 0)
            {
                ok = false;
                foreach (var s in plan.Issues) Debug.LogError($"[BootstrapGraph] {s}");
            }

            // total count
            int total = 0;
            foreach (var _ in plan.Ordered) total++;
            if (total <= 0) total = 1;

            var caps = BuildCapabilities(plan, profile, platform, isEditor);

            progress?.Report(new BootstrapProgress(
                normalized: 0f,
                local: 0f,
                index: 0,
                total: total,
                featureId: null,
                enabled: true,
                phase: BootstrapProgressPhase.Start,
                message: "Bootstrap start",
                exception: null));

            int i = 0;
            foreach (var n in plan.Ordered)
            {
                var entry = n.Entry;
                var feature = n.Feature;

                var ov = FeatureUtils.BuildOverrideMap(entry.Overrides);
                var ctx0 = new FeatureContext(profile, platform, isEditor, ov, caps, progress: null);

                bool enabled = entry.Enabled && feature.IsEnabled(in ctx0);

                // Begin
                progress?.Report(new BootstrapProgress(
                    normalized: (float)i / total,
                    local: 0f,
                    index: i,
                    total: total,
                    featureId: feature.Id,
                    enabled: enabled,
                    phase: BootstrapProgressPhase.FeatureBegin,
                    message: enabled ? "Begin" : "Skipped",
                    exception: null));

                long ms = 0;
                Exception ex = null;

                // per-feature reporter injected into ctx.Progress
                var featureReporter = progress != null
                    ? new FeatureProgressReporter(progress, i, total, feature.Id, enabled)
                    : null;

                var ctx = new FeatureContext(profile, platform, isEditor, ov, caps, progress: featureReporter);

                if (enabled)
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        // feature 내부에서 ctx.Progress.Report(...) 가능
                        await feature.Initialize(root, ctx);
                    }
                    catch (Exception e)
                    {
                        ex = e;
                        ok = false;
                        Debug.LogException(e);

                        // error also reported
                        progress?.Report(new BootstrapProgress(
                            normalized: (float)i / total,
                            local: 0f,
                            index: i,
                            total: total,
                            featureId: feature.Id,
                            enabled: enabled,
                            phase: BootstrapProgressPhase.FeatureProgress,
                            message: "Exception",
                            exception: e));
                    }
                    sw.Stop();
                    ms = sw.ElapsedMilliseconds;
                }

                runs.Add(new FeatureRun(feature.Id, enabled, 0, ms, ex));

                // End
                progress?.Report(new BootstrapProgress(
                    normalized: (float)(i + 1) / total,
                    local: 1f,
                    index: i,
                    total: total,
                    featureId: feature.Id,
                    enabled: enabled,
                    phase: BootstrapProgressPhase.FeatureEnd,
                    message: "End",
                    exception: ex));

                i++;
            }

            progress?.Report(new BootstrapProgress(
                normalized: 1f,
                local: 1f,
                index: total,
                total: total,
                featureId: null,
                enabled: ok,
                phase: BootstrapProgressPhase.Complete,
                message: "Bootstrap complete",
                exception: null));

            return new Result(runs, ok);
        }

        private static IReadOnlyDictionary<Type, IFeatureCapability> BuildCapabilities(
            FeaturePlan plan,
            string profile,
            RuntimePlatform platform,
            bool isEditor)
        {
            var caps = new Dictionary<Type, IFeatureCapability>();

            foreach (var n in plan.Ordered)
            {
                var entry = n.Entry;
                var feature = n.Feature;

                var ov = FeatureUtils.BuildOverrideMap(entry.Overrides);
                var ctx0 = new FeatureContext(profile, platform, isEditor, ov, capabilities: null, progress: null);

                if (!entry.Enabled) continue;
                if (!feature.IsEnabled(in ctx0)) continue;

                if (feature is IProvideCapabilities p)
                    p.Provide(caps);
            }

            return caps;
        }
    }
}
