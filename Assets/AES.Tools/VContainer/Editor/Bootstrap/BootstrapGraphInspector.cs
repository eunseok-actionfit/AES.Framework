using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.VContainer.Bootstrap.Framework.Editor
{
    [CustomEditor(typeof(BootstrapGraph))]
    public sealed class BootstrapGraphInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var graph = (BootstrapGraph)target;
            EditorGUILayout.Space(8);

            if (GUILayout.Button("Open Graph Window"))
                BootstrapGraphEditorWindow.Open();

            if (GUILayout.Button("Validate All Profiles"))
            {
                foreach (var p in graph.Profiles ?? Array.Empty<FeatureProfile>())
                {
                    var issues = FeatureValidators.ValidateProfile(p);
                    if (issues.Count == 0) Debug.Log($"[BootstrapGraph:{p.ProfileName}] OK");
                    else foreach (var s in issues) Debug.LogError($"[BootstrapGraph:{p.ProfileName}] {s}");
                }
            }
        }

        /// <summary>
        /// profile 내 features 배열을 실제로 재정렬한다. (SerializedObject 기반 write-back)
        /// </summary>
        internal static void AutoSortProfile(BootstrapGraph graph, int profileIndex)
        {
            if (!graph) return;

            var so = new SerializedObject(graph);
            var profilesProp = so.FindProperty("profiles");
            if (profilesProp == null || profilesProp.arraySize == 0) return;

            profileIndex = Mathf.Clamp(profileIndex, 0, profilesProp.arraySize - 1);
            var pProp = profilesProp.GetArrayElementAtIndex(profileIndex);
            var featuresProp = pProp.FindPropertyRelative("features");
            if (featuresProp == null || featuresProp.arraySize == 0) return;

            // collect
            var entries = new List<(int Index, string Id, int Order, string[] Deps)>(featuresProp.arraySize);

            for (int i = 0; i < featuresProp.arraySize; i++)
            {
                var entryProp = featuresProp.GetArrayElementAtIndex(i);
                var fProp = entryProp.FindPropertyRelative("feature");
                var f = fProp.objectReferenceValue as AppFeatureSO;
                if (!f) continue;

                entries.Add((i, f.Id, f.Order, f.DependsOn ?? Array.Empty<string>()));
            }

            if (entries.Count <= 1) return;

            // topo sort (Kahn) stable by Order/Id
            var present = new HashSet<string>(entries.Select(x => x.Id), StringComparer.Ordinal);
            var indeg = entries.ToDictionary(x => x.Id, _ => 0, StringComparer.Ordinal);
            var depsMap = entries.ToDictionary(
                x => x.Id,
                x => x.Deps.Where(d => !string.IsNullOrWhiteSpace(d) && present.Contains(d)).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

            foreach (var kv in depsMap)
                indeg[kv.Key] = kv.Value.Length;

            var zero = entries
                .Where(x => indeg[x.Id] == 0)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => x.Id)
                .ToList();

            var result = new List<string>(entries.Count);

            while (zero.Count > 0)
            {
                var cur = zero[0];
                zero.RemoveAt(0);
                result.Add(cur);

                foreach (var id in depsMap.Keys)
                {
                    if (!depsMap[id].Contains(cur, StringComparer.Ordinal)) continue;
                    indeg[id]--;
                    if (indeg[id] == 0)
                    {
                        zero.Add(id);
                        zero = zero
                            .OrderBy(x =>
                            {
                                var e = entries.First(t => t.Id == x);
                                return e.Order;
                            })
                            .ThenBy(x => x, StringComparer.Ordinal)
                            .ToList();
                    }
                }
            }

            // cycle fallback
            if (result.Count != entries.Count)
            {
                result = entries
                    .OrderBy(x => x.Order)
                    .ThenBy(x => x.Id, StringComparer.Ordinal)
                    .Select(x => x.Id)
                    .ToList();
            }

            // 실제 SerializedProperty 배열 재배열 (feature id 기준)
            // 현재 배열에서 목표 순서대로 entryProp를 가져와 새 배열로 재구성한다.
            var newArray = new List<SerializedProperty>(result.Count);

            foreach (string id in result)
            {
                for (int j = 0; j < featuresProp.arraySize; j++)
                {
                    var entryProp = featuresProp.GetArrayElementAtIndex(j);
                    var fProp = entryProp.FindPropertyRelative("feature");
                    var f = fProp.objectReferenceValue as AppFeatureSO;
                    if (f && string.Equals(f.Id, id, StringComparison.Ordinal))
                    {
                        newArray.Add(entryProp.Copy());
                        break;
                    }
                }
            }

            // featuresProp를 재구성: 기존 삭제 후 copy 값들을 다시 넣기
            while (featuresProp.arraySize > 0) featuresProp.DeleteArrayElementAtIndex(0);
            featuresProp.arraySize = newArray.Count;

            for (int i = 0; i < newArray.Count; i++)
            {
                var dst = featuresProp.GetArrayElementAtIndex(i);

                // entry는 { feature, enabled, overrides } 구조이므로 각각 복사
                dst.FindPropertyRelative("feature").objectReferenceValue =
                    newArray[i].FindPropertyRelative("feature").objectReferenceValue;
                dst.FindPropertyRelative("enabled").boolValue =
                    newArray[i].FindPropertyRelative("enabled").boolValue;

                var srcOverrides = newArray[i].FindPropertyRelative("overrides");
                var dstOverrides = dst.FindPropertyRelative("overrides");
                if (srcOverrides != null && dstOverrides != null)
                {
                    dstOverrides.arraySize = srcOverrides.arraySize;
                    for (int k = 0; k < srcOverrides.arraySize; k++)
                    {
                        var s = srcOverrides.GetArrayElementAtIndex(k);
                        var d = dstOverrides.GetArrayElementAtIndex(k);
                        d.FindPropertyRelative("key").stringValue = s.FindPropertyRelative("key").stringValue;
                        d.FindPropertyRelative("value").objectReferenceValue = s.FindPropertyRelative("value").objectReferenceValue;
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
        }

        internal static void AddMissingDependencies(FeatureProfile profile)
        {
            var entries = profile.Features?.Where(e => e != null && e.Feature != null).ToList();
            if (entries == null || entries.Count == 0) return;

            var present = new HashSet<string>(entries.Select(e => e.Feature.Id), StringComparer.Ordinal);

            foreach (var e in entries)
            {
                var f = e.Feature;
                foreach (var dep in f.DependsOn ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(dep)) continue;
                    if (!present.Contains(dep))
                        Debug.LogError($"[BootstrapGraph] Missing dep entry: {f.Id} depends on {dep}. Add a feature with Id='{dep}' to this profile.");
                }
            }
        }
    }
}
