#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    public class BindingDebuggerWindow : EditorWindow
    {
        [MenuItem("AES/DataBinding/Binding Debugger Window")]
        static void Open() => GetWindow<BindingDebuggerWindow>("Binding Debugger");

        Vector2 _scroll;

        // ░░░ Column Widths (Resizable, Stored) ░░░
        static readonly string[] _colKeys =
        {
            "AES_BD_Col_Binding", "AES_BD_Col_Ctx", "AES_BD_Col_Path",
            "AES_BD_Col_Full", "AES_BD_Col_Value", "AES_BD_Col_Updates",
            "AES_BD_Col_Frame", "AES_BD_Col_Err"
        };

        // 기본 컬럼 폭 (더블클릭으로 리셋 시 사용)
        static readonly float[] _defaultWidth =
        {
            120, 90, 140, 180, 150, 55, 85, 25
        };

        float[] _colWidth = new float[8];

        const float MinCol = 40f;

        class BindInfo
        {
            public BindingBehaviour binding;
            public string providerObj;
            public string providerType;
            public string runtimeCtxType;
            public string ctxName;
            public string path;
            public string fullPath;
            public string value;
            public string error;
            public int count;
            public int frameSub;
            public int frameUpd;

            public bool hasError;
            public bool hasWarning;
            public string warnMessage;
        }

        readonly Dictionary<string, bool> _foldouts = new();

        string _filterText = "";
        bool _onlyErrors = false;
        bool _autoRefresh = true;
        bool _zebra = true; // 지브라 로우 On/Off

        double _lastAutoRepaint;

        // Context 상속 이슈 출력용
        readonly List<string> _contextIssues = new();

        // MonoContext 내부 정보용 Reflection 캐시
        static readonly FieldInfo _fiViewModelSource =
            typeof(MonoContext).GetField("viewModelSource", BindingFlags.NonPublic | BindingFlags.Instance);

        static readonly MethodInfo _miEditorResolveParent =
            typeof(MonoContext).GetMethod("EditorResolveParentProvider",
                BindingFlags.NonPublic | BindingFlags.Instance);

        void OnEnable()
        {
            LoadColumnWidths();
        }

        void OnDisable()
        {
            SaveColumnWidths();
        }

        //──────────────────────────────────────────────────────────────
        //  Collect BindInfo
        //──────────────────────────────────────────────────────────────
        List<BindInfo> CollectBindingInfos(BindingBehaviour[] bindings)
        {
            var list = new List<BindInfo>();

            foreach (var b in bindings)
            {
                var so = new SerializedObject(b);

                var info = new BindInfo
                {
                    binding       = b,
                    ctxName       = so.FindProperty("_debugContextName")?.stringValue ?? "(unknown)",
                    path          = so.FindProperty("_debugMemberPath")?.stringValue ?? "",
                    value         = so.FindProperty("_debugLastValue")?.stringValue ?? "",
                    error         = so.FindProperty("_debugLastError")?.stringValue ?? "",
                    count         = so.FindProperty("_debugUpdateCount")?.intValue ?? 0,
                    providerObj   = so.FindProperty("_debugProviderObject")?.stringValue ?? "",
                    providerType  = so.FindProperty("_debugProviderType")?.stringValue ?? "",
                    runtimeCtxType= so.FindProperty("_debugRuntimeContextType")?.stringValue ?? "",
                    fullPath      = so.FindProperty("_debugFullPath")?.stringValue ?? "",
                    frameSub      = so.FindProperty("_debugFrameSubscribed")?.intValue ?? 0,
                    frameUpd      = so.FindProperty("_debugFrameFirstUpdate")?.intValue ?? 0,
                };

                AnalyzeIssues(info);
                list.Add(info);
            }

            return list;
        }

        //──────────────────────────────────────────────────────────────
        //  Filters
        //──────────────────────────────────────────────────────────────
        void ApplyFilters(List<BindInfo> list)
        {
            if (_onlyErrors)
                list.RemoveAll(i => !i.hasError);

            if (!string.IsNullOrEmpty(_filterText))
            {
                string f = _filterText.ToLowerInvariant();
                list.RemoveAll(i =>
                    !(i.binding.name.ToLowerInvariant().Contains(f) ||
                      i.binding.GetType().Name.ToLowerInvariant().Contains(f) ||
                      i.ctxName.ToLowerInvariant().Contains(f) ||
                      i.providerObj.ToLowerInvariant().Contains(f) ||
                      i.providerType.ToLowerInvariant().Contains(f) ||   // ← providerType 도 필터에 포함
                      i.path.ToLowerInvariant().Contains(f) ||
                      i.fullPath.ToLowerInvariant().Contains(f)));
            }
        }

        //──────────────────────────────────────────────────────────────
        //  OnGUI
        //──────────────────────────────────────────────────────────────
        void OnGUI()
        {
            if (!BindingDebugSettings.Enabled)
            {
                EditorGUILayout.HelpBox(
                    "Binding Debug is disabled.\nEnable: AES/DataBinding/Binding Debug",
                    MessageType.Info);

                if (GUILayout.Button("Enable Binding Debug"))
                    BindingDebugSettings.Enabled = true;

                return;
            }

            DrawToolbar();

            // Context 이슈 표시
            if (_contextIssues.Count > 0)
            {
                foreach (var msg in _contextIssues)
                    EditorGUILayout.HelpBox(msg, MessageType.Warning);

                EditorGUILayout.Space(4);
            }

            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastAutoRepaint > 0.5f)
            {
                _lastAutoRepaint = EditorApplication.timeSinceStartup;
                Repaint();
            }

            // Collect binding data
            var bindings = FindObjectsByType<BindingBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            var list = CollectBindingInfos(bindings);

            // Apply Filters
            ApplyFilters(list);

            // Sorting (providerObj → providerType → Binding 타입 → path)
            list = list
                .OrderBy(i => string.IsNullOrEmpty(i.providerObj)  ? "~" : i.providerObj)
                .ThenBy(i => string.IsNullOrEmpty(i.providerType) ? "~" : i.providerType)
                .ThenBy(i => i.binding.GetType().Name)
                .ThenBy(i => i.path)
                .ToList();

            var groups = list
                .GroupBy(i => string.IsNullOrEmpty(i.providerObj)
                    ? "(No Provider)"
                    : i.providerObj);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var g in groups) { DrawGroup(g.Key, g.ToList()); }

            EditorGUILayout.EndScrollView();
        }

        //──────────────────────────────────────────────────────────────
        //  Toolbar
        //──────────────────────────────────────────────────────────────
        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Repaint();

            GUILayout.Label("Filter:", GUILayout.Width(40));
            _filterText = GUILayout.TextField(_filterText, EditorStyles.toolbarTextField, GUILayout.MinWidth(100));

            _onlyErrors = GUILayout.Toggle(_onlyErrors, "Only Errors", EditorStyles.toolbarButton, GUILayout.Width(90));
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));
            _zebra = GUILayout.Toggle(_zebra, "Zebra", EditorStyles.toolbarButton, GUILayout.Width(70));

            if (GUILayout.Button("Check Contexts", EditorStyles.toolbarButton, GUILayout.Width(110))) { ScanContextIssues(); }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        //──────────────────────────────────────────────────────────────
        //  Issue Analysis (Binding 단위)
        //──────────────────────────────────────────────────────────────
        void AnalyzeIssues(BindInfo info)
        {
            info.hasError = !string.IsNullOrEmpty(info.error);
            info.hasWarning = false;

            if (info.hasError) return;
            if (!EditorApplication.isPlaying) return;

            bool hasProvider = !string.IsNullOrEmpty(info.providerObj);

            if (hasProvider && string.IsNullOrEmpty(info.runtimeCtxType))
            {
                info.hasWarning = true;
                info.warnMessage = "RuntimeContext missing.\n- Parent not initialized\n- Missing SetViewModel";
                return;
            }

            bool isPath = info.binding is ContextBindingBase;
            if (!isPath) return;

            if (!string.IsNullOrEmpty(info.runtimeCtxType) && info.count == 0)
            {
                info.hasWarning = true;
                info.warnMessage = "Binding never updated (count=0)\nPossible wrong path.";
            }
        }

        //──────────────────────────────────────────────────────────────
        //  Groups
        //──────────────────────────────────────────────────────────────
        void DrawGroup(string key, List<BindInfo> list)
        {
            if (!_foldouts.ContainsKey(key))
                _foldouts[key] = true;

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginVertical("box");

            string label = $"{key} ({list.Count} bindings)";
            _foldouts[key] = EditorGUILayout.Foldout(_foldouts[key], label, true);

            if (_foldouts[key])
            {
                DrawHeader();

                int rowIndex = 0;

                foreach (var i in list)
                {
                    DrawRow(i, rowIndex);
                    rowIndex++;
                }
            }

            EditorGUILayout.EndVertical();
        }

        //──────────────────────────────────────────────────────────────
        //  Header with Column Resize
        //──────────────────────────────────────────────────────────────
        void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(4, 4, 2, 2),
                margin = new RectOffset(2, 2, 1, 3)
            };

            EditorGUILayout.BeginHorizontal(headerStyle);

            DrawHeaderCell("Binding", 0);
            DrawHeaderCell("Ctx", 1);
            DrawHeaderCell("Path", 2);
            DrawHeaderCell("FullPath", 3);
            DrawHeaderCell("Value", 4);
            DrawHeaderCell("Upd", 5);
            DrawHeaderCell("F(S/U)", 6);
            DrawHeaderCell("Err", 7);

            EditorGUILayout.EndHorizontal();
        }

        void DrawHeaderCell(string title, int idx)
        {
            Rect r = GUILayoutUtility.GetRect(
                new GUIContent(title),
                EditorStyles.boldLabel,
                GUILayout.Width(_colWidth[idx]));

            EditorGUI.LabelField(r, title, EditorStyles.boldLabel);

            // 오른쪽에 작은 드래그 핸들
            Rect drag = new Rect(r.xMax - 4, r.y, 8, r.height);
            EditorGUIUtility.AddCursorRect(drag, MouseCursor.ResizeHorizontal);

            int id = idx + 5000;
            Event e = Event.current;

            // 더블클릭 시 해당 컬럼 폭을 기본값으로 리셋
            if (e.type == EventType.MouseDown && drag.Contains(e.mousePosition) && e.button == 0 && e.clickCount == 2)
            {
                ResetColumnWidth(idx);
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && drag.Contains(e.mousePosition) && e.button == 0)
            {
                GUIUtility.hotControl = id;
                e.Use();
            }
            else if (GUIUtility.hotControl == id)
            {
                if (e.type == EventType.MouseDrag)
                {
                    _colWidth[idx] += e.delta.x;
                    _colWidth[idx] = Mathf.Max(MinCol, _colWidth[idx]);
                    SaveColumnWidths();
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseUp)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
            }
        }

        void ResetColumnWidth(int idx)
        {
            if (idx < 0 || idx >= _colWidth.Length) return;
            _colWidth[idx] = _defaultWidth[idx];
            SaveColumnWidths();
            Repaint();
        }

        //──────────────────────────────────────────────────────────────
        //  Row (Compact + Strong Zebra)
        //──────────────────────────────────────────────────────────────
        void DrawRow(BindInfo i, int rowIndex)
        {
            var rowStyle = new GUIStyle("box")
            {
                padding = new RectOffset(4, 4, 2, 2),
                margin  = new RectOffset(2, 2, 1, 1)
            };

            Color prevBg = GUI.backgroundColor;

            if (_zebra && !i.hasError && !i.hasWarning)
            {
                GUI.backgroundColor = (rowIndex % 2 == 0)
                    ? new Color(0.80f, 0.88f, 1.00f)
                    : new Color(0.90f, 0.95f, 1.00f);
            }

            if (i.hasError)
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            else if (i.hasWarning)
                GUI.backgroundColor = new Color(1f, 0.95f, 0.6f);

            EditorGUILayout.BeginVertical(rowStyle);
            GUI.backgroundColor = prevBg;

            // 메인 행
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(i.binding, typeof(BindingBehaviour), true, GUILayout.Width(_colWidth[0]));
            LabelEllipsis(i.ctxName, _colWidth[1], 20);
            LabelEllipsis(i.path, _colWidth[2], 30);
            LabelEllipsis(i.fullPath, _colWidth[3], 40);
            LabelEllipsis(i.value, _colWidth[4], 40);

            Label(i.count.ToString(), _colWidth[5]);

            string fSU = (i.frameSub != 0 || i.frameUpd != 0)
                ? $"{i.frameSub}/{i.frameUpd}"
                : "-";

            Label(fSU, _colWidth[6]);
            Label(i.hasError ? "E" : i.hasWarning ? "W" : "", _colWidth[7]);

            EditorGUILayout.EndHorizontal();

            // Error / Warning message
            if (i.hasError || i.hasWarning)
            {
                GUI.color = i.hasError ? Color.red : new Color(0.55f, 0.45f, 0.1f);
                EditorGUILayout.LabelField(i.hasError ? i.error : i.warnMessage, EditorStyles.wordWrappedMiniLabel);
                GUI.color = Color.white;
            }

            // Buttons inside same row
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Select Binding", GUILayout.Width(110)))
                Selection.activeObject = i.binding;

            if (!string.IsNullOrEmpty(i.providerObj))
            {
                string btnLabel = "Select Provider";
                string tooltip = string.IsNullOrEmpty(i.providerType)
                    ? "Select provider object"
                    : $"Select provider object\nType: {i.providerType}";

                if (GUILayout.Button(new GUIContent(btnLabel, tooltip), GUILayout.Width(110)))
                    SelectProvider(i);
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        //──────────────────────────────────────────────────────────────
        //  Helper UI
        //──────────────────────────────────────────────────────────────
        void Label(string s, float w)
        {
            EditorGUILayout.LabelField(string.IsNullOrEmpty(s) ? "-" : s, GUILayout.Width(w));
        }

        void LabelEllipsis(string s, float w, int max)
        {
            if (string.IsNullOrEmpty(s))
            {
                EditorGUILayout.LabelField("-", GUILayout.Width(w));
                return;
            }

            string disp = s.Length > max ? s[..max] + "…" : s;
            EditorGUILayout.LabelField(new GUIContent(disp, s), GUILayout.Width(w));
        }

        //──────────────────────────────────────────────────────────────
        //  Provider Selection
        //──────────────────────────────────────────────────────────────
        void SelectProvider(BindInfo i)
        {
#if UNITY_2022_2_OR_NEWER
            var all = Object.FindObjectsByType<MonoContext>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
            var all = Object.FindObjectsOfType<MonoContext>(true);
#endif

            if (all == null || all.Length == 0)
                return;

            MonoContext found = null;

            // 1) providerType 이 있으면 타입까지 우선 매칭
            if (!string.IsNullOrEmpty(i.providerType))
            {
                found = all.FirstOrDefault(mc =>
                    (mc.gameObject.name == i.providerObj ||
                     mc.ContextName == i.providerObj) &&
                    mc.GetType().FullName == i.providerType);
            }

            // 2) 타입으로 못 찾았으면, 기존대로 이름/ContextName 만 사용
            if (found == null)
            {
                found = all.FirstOrDefault(mc =>
                    mc.gameObject.name == i.providerObj ||
                    mc.ContextName == i.providerObj);
            }

            if (found)
                Selection.activeObject = found;
        }

        //──────────────────────────────────────────────────────────────
        //  Column Width Persistence
        //──────────────────────────────────────────────────────────────
        void LoadColumnWidths()
        {
            for (int i = 0; i < 8; i++)
                _colWidth[i] = EditorPrefs.GetFloat(_colKeys[i], _defaultWidth[i]);
        }

        void SaveColumnWidths()
        {
            for (int i = 0; i < 8; i++)
                EditorPrefs.SetFloat(_colKeys[i], _colWidth[i]);
        }

        //──────────────────────────────────────────────────────────────
        //  Context 상속 정합성 검사
        //──────────────────────────────────────────────────────────────
        void ScanContextIssues()
        {
            _contextIssues.Clear();

           #if UNITY_2022_2_OR_NEWER
            var all = Object.FindObjectsByType<MonoContext>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
var all = Object.FindObjectsOfType<MonoContext>(true);
#endif

            if (all == null || all.Length == 0)
                return;

            if (_fiViewModelSource == null || _miEditorResolveParent == null)
            {
                _contextIssues.Add(
                    "MonoContext 내부 정보를 Reflection 으로 가져오지 못했습니다.\n" +
                    "- viewModelSource / EditorResolveParentProvider 시그니처를 확인하세요.");

                return;
            }

            // InheritFromParent 들만 대상으로 부모 맵 구성
            var parentMap = new Dictionary<MonoContext, MonoContext>();

            foreach (var mc in all)
            {
                var srcObj = _fiViewModelSource.GetValue(mc);
                if (srcObj is not ViewModelSourceMode src)
                    continue;

                if (src != ViewModelSourceMode.InheritFromParent)
                    continue;

                var parentProv = _miEditorResolveParent.Invoke(mc, null) as IBindingContextProvider;

                if (parentProv == null)
                {
                    _contextIssues.Add(
                        $"[Context:{mc.name}] ViewModelSource=InheritFromParent 이지만 " +
                        "부모 컨텍스트를 찾지 못했습니다.\n" +
                        "- inheritLookupMode / inheritContextName 설정을 확인하세요.");

                    continue;
                }

                var parentMc = parentProv as MonoContext;
                if (parentMc != null)
                    parentMap[mc] = parentMc;
            }

            if (parentMap.Count == 0)
                return;

            // 사이클 탐지 (단순 DFS)
            var visited = new HashSet<MonoContext>();
            var stack = new Stack<MonoContext>();
            var cycleReported = new HashSet<MonoContext>();

            foreach (var mc in parentMap.Keys)
            {
                if (!visited.Contains(mc))
                    Dfs(mc);
            }

            void Dfs(MonoContext node)
            {
                if (stack.Contains(node))
                {
                    // stack 안에서 cycle 부분만 뽑아서 메시지 생성
                    var arr = stack.Reverse().SkipWhile(x => x != node).ToList();
                    arr.Add(node);

                    // 같은 사이클에 대해 여러 번 찍지 않도록 가드
                    if (arr.Any(cycleReported.Contains))
                        return;

                    foreach (var n in arr)
                        cycleReported.Add(n);

                    string names = string.Join(" -> ", arr.Select(m => m.name));
                    _contextIssues.Add(
                        "InheritFromParent 컨텍스트 상속에 순환 참조가 감지되었습니다.\n" +
                        $"Cycle: {names}\n" +
                        "- 서로를 부모로 가리키는 MonoContext 가 있는지 확인하세요.");

                    return;
                }

                if (visited.Contains(node))
                    return;

                visited.Add(node);
                stack.Push(node);

                if (parentMap.TryGetValue(node, out var parent) && parent != null)
                    Dfs(parent);

                stack.Pop();
            }
        }
    }
}
#endif