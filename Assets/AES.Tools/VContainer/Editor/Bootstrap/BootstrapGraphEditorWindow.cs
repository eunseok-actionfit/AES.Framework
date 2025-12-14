using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AES.Tools.VContainer.Bootstrap.Framework.Editor
{
    public sealed class BootstrapGraphEditorWindow : EditorWindow
    {
        private BootstrapGraph graph;
        private int profileIndex;

        private SerializedObject graphSO;
        private SerializedProperty profilesProp;
        private SerializedProperty featuresProp;

        private ReorderableList list;

        // 바인딩 안정화
        private int boundProfileIndex = -1;
        private SerializedProperty boundFeaturesProp;

        private Vector2 scroll;

        private string search;
        private bool showOnlyIssues;
        private bool showOnlyEnabled = true;

        // 필터 row 접기/펼치기 상태
        private readonly Dictionary<string, bool> revealFiltered = new Dictionary<string, bool>(StringComparer.Ordinal);

        // cycle 노드 강조용
        private HashSet<string> cycleNodes;

        [MenuItem("AES/Bootstrap/Graph Window")]
        public static void Open() => GetWindow<BootstrapGraphEditorWindow>("Bootstrap Graph");

        private void OnGUI()
        {
            DrawHeader();

            if (!graph)
            {
                EditorGUILayout.HelpBox("Assign a BootstrapGraph asset.", MessageType.Info);
                return;
            }

            BindSerialized();

            if (profilesProp == null || profilesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No profiles in this graph.", MessageType.Warning);
                return;
            }

            DrawProfilePicker();

            if (featuresProp == null)
            {
                EditorGUILayout.HelpBox("Profile has no features array.", MessageType.Error);
                return;
            }
            
            if (boundProfileIndex != profileIndex || boundFeaturesProp != featuresProp)
            {
                boundProfileIndex = profileIndex;
                boundFeaturesProp = featuresProp;
                list = null;
            }

            graphSO.Update();
            EnsureList();

            // plan(사이클/이슈)
            var profiles = graph.Profiles?.ToArray();
            var profile = (profiles != null && profiles.Length > 0)
                ? profiles[Mathf.Clamp(profileIndex, 0, profiles.Length - 1)]
                : null;

            var plan = profile != null ? FeaturePlanner.Build(profile) : null;
            cycleNodes = BuildCycleNodesFromIssues(plan);

            DrawToolbar(plan);

            if (plan != null && plan.Issues != null && plan.Issues.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", plan.Issues), MessageType.Error);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            list.DoLayoutList();
            EditorGUILayout.EndScrollView();

            graphSO.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var newGraph = (BootstrapGraph)EditorGUILayout.ObjectField(graph, typeof(BootstrapGraph), false, GUILayout.Width(280));
                if (newGraph != graph)
                {
                    graph = newGraph;
                    graphSO = null;
                    list = null;
                    boundProfileIndex = -1;
                    boundFeaturesProp = null;
                    revealFiltered.Clear();
                    cycleNodes = null;
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();

                var searchStyle =
                    GUI.skin.FindStyle("ToolbarSearchTextField") ??
                    GUI.skin.FindStyle("ToolbarSeachTextField") ??
                    EditorStyles.toolbarTextField;

                var cancelStyle =
                    GUI.skin.FindStyle("ToolbarSearchCancelButton") ??
                    GUI.skin.FindStyle("ToolbarSeachCancelButton");

                search ??= "";
                search = GUILayout.TextField(search, searchStyle, GUILayout.Width(260));

                if (cancelStyle != null)
                {
                    if (GUILayout.Button(GUIContent.none, cancelStyle))
                    {
                        search = "";
                        GUI.FocusControl(null);
                    }
                }
                else
                {
                    if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22)))
                    {
                        search = "";
                        GUI.FocusControl(null);
                    }
                }
            }
        }

        private void BindSerialized()
        {
            if (!graph) return;
            if (graphSO != null && graphSO.targetObject == graph) return;

            graphSO = new SerializedObject(graph);
            profilesProp = graphSO.FindProperty("profiles");
            list = null;
            boundProfileIndex = -1;
            boundFeaturesProp = null;
            revealFiltered.Clear();
        }

        private void DrawProfilePicker()
        {
            var profiles = graph.Profiles?.ToArray();
            var names = profiles?.Select(p => p.ProfileName).ToArray() ?? Array.Empty<string>();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Profile", GUILayout.Width(50));
                profileIndex = Mathf.Clamp(profileIndex, 0, Math.Max(0, names.Length - 1));
                var newIndex = EditorGUILayout.Popup(profileIndex, names);

                if (newIndex != profileIndex)
                {
                    profileIndex = newIndex;
                    list = null;
                    boundProfileIndex = -1;
                    boundFeaturesProp = null;
                    revealFiltered.Clear();
                }

                GUILayout.FlexibleSpace();
            }

            var pProp = profilesProp.GetArrayElementAtIndex(profileIndex);
            featuresProp = pProp.FindPropertyRelative("features");
        }

        private void EnsureList()
        {
            if (list != null) return;

            list = new ReorderableList(graphSO, featuresProp, draggable: true, displayHeader: true, displayAddButton: true, displayRemoveButton: true);

            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Features (drag to reorder)  |  Enabled toggle  |  Fix MissingFeature");
            };

            list.onAddDropdownCallback = (_, _) =>
            {
                BootstrapGraphQuickAdd.ShowCreateAddMenu(graph, profileIndex);
                graphSO.Update();
            };

            list.onRemoveCallback = rl =>
            {
                if (EditorUtility.DisplayDialog("Remove Feature", "Remove selected entry from profile?", "Remove", "Cancel"))
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(rl);
                    graphSO.ApplyModifiedProperties();
                    EditorUtility.SetDirty(graph);
                    AssetDatabase.SaveAssets();
                }
            };

            // ✅ 필터 row는 1줄(접기바), 나머지는 2줄
            list.elementHeightCallback = index =>
            {
                var entryProp = featuresProp.GetArrayElementAtIndex(index);
                var feature = entryProp.FindPropertyRelative("feature").objectReferenceValue as AppFeatureSO;
                var enabled = entryProp.FindPropertyRelative("enabled").boolValue;

                string key = GetRowKey(index, feature);
                bool pass = PassFilter(feature, enabled);
                bool revealed = revealFiltered.TryGetValue(key, out var r) && r;

                if (!pass && !revealed)
                    return EditorGUIUtility.singleLineHeight + 6f;

                return EditorGUIUtility.singleLineHeight * 2f + 8f;
            };

            list.drawElementCallback = (rect, index, _, _) =>
            {
                var entryProp = featuresProp.GetArrayElementAtIndex(index);
                var featureProp = entryProp.FindPropertyRelative("feature");
                var enabledProp = entryProp.FindPropertyRelative("enabled");
                var feature = featureProp.objectReferenceValue as AppFeatureSO;

                bool entryEnabled = enabledProp.boolValue;

                string key = GetRowKey(index, feature);
                bool pass = PassFilter(feature, entryEnabled);
                bool revealed = revealFiltered.TryGetValue(key, out var r) && r;

                rect.y += 2f;

                bool isMissing = feature && feature.GetType().Name == "MissingFeature";
                bool isCycle = feature && cycleNodes != null && cycleNodes.Contains(feature.Id);

                // 배경 강조
                if (isCycle)
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y - 1, rect.width, rect.height + 2), new Color(1f, 0.88f, 0.88f));

                if (isMissing)
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y - 1, rect.width, rect.height + 2), new Color(1f, 0.55f, 0.55f));

                // ===== 필터로 숨겨진 row: 접기바만 =====
                if (!pass && !revealed)
                {
                    DrawFilteredCollapsedRow(rect, key, feature);
                    return;
                }

                // ===== 펼친 row (필터 통과하지 못한 경우는 회색 처리 + 접기 가능) =====
                bool drawDisabled = !pass && revealed;

                if (drawDisabled)
                {
                    var bar = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                    var foldRect = new Rect(bar.x, bar.y, 18, bar.height);

                    bool newReveal = EditorGUI.Foldout(foldRect, true, GUIContent.none, toggleOnLabelClick: false);
                    if (!newReveal) revealFiltered[key] = false;

                    // 나머지 영역에 "(filtered)" 표시
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUI.LabelField(new Rect(bar.x + 18, bar.y, bar.width - 18, bar.height),
                            feature ? $"{feature.Id} (filtered)" : "<null feature> (filtered)",
                            EditorStyles.miniLabel);
                    }
                }

                using (new EditorGUI.DisabledScope(drawDisabled))
                {
                    var prev = GUI.color;
                    if (drawDisabled) GUI.color = new Color(1f, 1f, 1f, 0.35f);

                    DrawNormalRow(rect, entryProp, featureProp, enabledProp, feature, isMissing);

                    GUI.color = prev;
                }
            };
        }

        private void DrawFilteredCollapsedRow(Rect rect, string key, AppFeatureSO feature)
        {
            var bar = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            var foldRect = new Rect(bar.x, bar.y, 18, bar.height);

            bool open = EditorGUI.Foldout(foldRect, false, GUIContent.none, toggleOnLabelClick: false);
            if (open) revealFiltered[key] = true;

            using (new EditorGUI.DisabledScope(true))
            {
                var labelRect = new Rect(bar.x + 18, bar.y, bar.width - 18, bar.height);
                EditorGUI.LabelField(labelRect,
                    feature ? $"{feature.Id} (filtered)" : "<null feature> (filtered)",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawNormalRow(
            Rect rect,
            SerializedProperty entryProp,
            SerializedProperty featureProp,
            SerializedProperty enabledProp,
            AppFeatureSO feature,
            bool isMissingFeature)
        {
            var r0 = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            var r1 = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2f, rect.width, EditorGUIUtility.singleLineHeight);

            // line1: enabled + object
            var toggleRect = new Rect(r0.x, r0.y, 18, r0.height);
            enabledProp.boolValue = EditorGUI.Toggle(toggleRect, enabledProp.boolValue);

            var objRect = new Rect(r0.x + 22, r0.y, r0.width - 22, r0.height);
            EditorGUI.PropertyField(objRect, featureProp, GUIContent.none);

            // line2
            if (!feature)
            {
                EditorGUI.HelpBox(r1, "Null feature reference", MessageType.Error);
                return;
            }

            var id = feature.Id;
            var order = feature.Order;
            var cat = BootstrapGraphQuickAdd.GetCategory(feature.GetType());

            bool hasMissingDep = HasMissingDepsInProfile(id, feature.DependsOn);
            if (hasMissingDep)
                EditorGUI.LabelField(new Rect(r1.x, r1.y, 70, r1.height), "MISSING", EditorStyles.miniBoldLabel);

            var idRect = new Rect(r1.x + 72, r1.y, 220, r1.height);
            if (GUI.Button(idRect, id, EditorStyles.linkLabel))
            {
                EditorGUIUtility.PingObject(feature);
                Selection.activeObject = feature;
            }

            EditorGUI.LabelField(new Rect(r1.x + 300, r1.y, 240, r1.height), $"Order {order} | {cat}", EditorStyles.miniLabel);

            // 오른쪽 버튼 영역
            var right = new Rect(r1.xMax - 170, r1.y, 170, r1.height);
            float w = 75f;

            if (isMissingFeature)
            {
                // ✅ Fix 고정
                var fixRect = new Rect(right.x, right.y, w, right.height);
                if (GUI.Button(fixRect, "Fix…"))
                    ShowFixMenu(entryProp, feature.Id);

                var pingRect = new Rect(right.x + w + 6, right.y, w, right.height);
                if (GUI.Button(pingRect, "Ping"))
                {
                    EditorGUIUtility.PingObject(feature);
                    Selection.activeObject = feature;
                }
                return;
            }

            var b0 = new Rect(right.x, right.y, w, right.height);
            var b1 = new Rect(right.x + w + 6, right.y, w, right.height);

            if (GUI.Button(b0, "Ping"))
            {
                EditorGUIUtility.PingObject(feature);
                Selection.activeObject = feature;
            }

            if (GUI.Button(b1, "Select"))
                Selection.activeObject = feature;
        }

        private void DrawToolbar(FeaturePlan plan)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    var profiles = graph.Profiles?.ToArray();
                    var profile = (profiles != null && profiles.Length > 0) ? profiles[profileIndex] : null;
                    var issues = FeatureValidators.ValidateProfile(profile);
                    if (issues.Count == 0) Debug.Log("[BootstrapGraph] No issues.");
                    else foreach (var s in issues) Debug.LogError($"[BootstrapGraph] {s}");
                }

                if (GUILayout.Button("Sort", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    BootstrapGraphInspector.AutoSortProfile(graph, profileIndex);
                    graphSO.Update();
                    
                    list = null;
                    boundFeaturesProp = null;
                    revealFiltered.Clear();

                    Repaint();
                }

                GUILayout.Space(10);

                showOnlyEnabled = GUILayout.Toggle(showOnlyEnabled, "Enabled", EditorStyles.toolbarButton, GUILayout.Width(70));
                showOnlyIssues = GUILayout.Toggle(showOnlyIssues, "Only Issues", EditorStyles.toolbarButton, GUILayout.Width(90));

                GUILayout.FlexibleSpace();

                if (plan != null && plan.HasCycle)
                    GUILayout.Label("CYCLE", EditorStyles.toolbarButton);
            }
        }

        private bool PassFilter(AppFeatureSO feature, bool entryEnabled)
        {
            if (showOnlyEnabled && !entryEnabled) return false;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                if (feature)
                {
                    if (feature.Id.IndexOf(s, StringComparison.OrdinalIgnoreCase) < 0 &&
                        feature.name.IndexOf(s, StringComparison.OrdinalIgnoreCase) < 0 &&
                        feature.GetType().Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) < 0)
                        return false;
                }
                else return false;
            }

            if (showOnlyIssues && feature)
            {
                if (!HasMissingDepsInProfile(feature.Id, feature.DependsOn)) return false;
            }

            return true;
        }

        private bool HasMissingDepsInProfile(string selfId, string[] deps)
        {
            if (deps == null || deps.Length == 0) return false;

            var present = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < featuresProp.arraySize; i++)
            {
                var e = featuresProp.GetArrayElementAtIndex(i);
                var f = e.FindPropertyRelative("feature").objectReferenceValue as AppFeatureSO;
                if (f) present.Add(f.Id);
            }

            foreach (var d in deps)
            {
                if (string.IsNullOrWhiteSpace(d)) continue;
                if (!present.Contains(d)) return true;
            }

            return false;
        }

        private void ShowFixMenu(SerializedProperty entryProp, string forcedId)
        {
            var types = BootstrapGraphQuickAdd.FindAllFeatureTypes()
                .Where(t => t.Name != "MissingFeature")
                .ToArray();

            var menu = new GenericMenu();

            foreach (var t in types)
            {
                var cat = BootstrapGraphQuickAdd.GetCategory(t);
                menu.AddItem(new GUIContent($"{cat}/{t.Name}"), false, () =>
                {
                    var asset = FindOrCreateByForcedId(t, forcedId);
                    if (!asset) return;

                    entryProp.FindPropertyRelative("feature").objectReferenceValue = asset;

                    graphSO.ApplyModifiedProperties();
                    EditorUtility.SetDirty(graph);
                    AssetDatabase.SaveAssets();

                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                });
            }

            menu.ShowAsContext();
        }

        private AppFeatureSO FindOrCreateByForcedId(Type featureType, string forcedId)
        {
            var existing = BootstrapGraphQuickAdd.FindExistingById(forcedId);
            if (existing) return existing;

            var created = BootstrapGraphQuickAdd.CreateFeatureAsset(featureType, forcedId);
            if (!created) return null;

            // id 강제 세팅(serialize field: "id")
            var so = new SerializedObject(created);
            var idProp = so.FindProperty("id");
            if (idProp != null) idProp.stringValue = forcedId;
            so.ApplyModifiedPropertiesWithoutUndo();

            created.name = forcedId;

            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();

            return created;
        }

        private static string GetRowKey(int index, AppFeatureSO feature)
        {
            if (feature) return feature.Id;
            return $"<null:{index}>";
        }

        // FeaturePlan에 CyclePath 프로퍼티가 없을 수도 있으니, Issues에서 "Cycle path:"를 파싱해서 사용
        private static HashSet<string> BuildCycleNodesFromIssues(FeaturePlan plan)
        {
            if (plan == null || plan.Issues == null) return null;

            string line = null;
            for (int i = 0; i < plan.Issues.Count; i++)
            {
                var s = plan.Issues[i];
                if (s != null && s.StartsWith("Cycle path:", StringComparison.OrdinalIgnoreCase))
                {
                    line = s;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(line)) return null;

            var idx = line.IndexOf(':');
            if (idx < 0 || idx + 1 >= line.Length) return null;

            var rhs = line.Substring(idx + 1).Trim();
            var parts = rhs.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            if (parts.Length == 0) return null;
            return new HashSet<string>(parts, StringComparer.Ordinal);
        }
    }
}
