#if UNITY_EDITOR
using System.Collections.Generic;
using AES.Tools;
using AES.Tools.Debugging;
using UnityEditor;
using UnityEngine;

public sealed class StateMachineGraphWindow : EditorWindow
{
    IStateMachineOwner _owner;
    StateMachine       _machine;

    readonly List<StateMachine.StateInfo>      _stateInfos  = new();
    readonly List<StateMachine.TransitionInfo> _transInfos  = new();

    // 노드 위치 (월드 좌표, zoom 적용 전 기준)
    readonly Dictionary<string, Vector2> _nodePositions = new();

    Vector2 _scroll;
    float   _zoom = 1f;

    StateMachineGraphAsset _saveTarget;

    // 드래그 상태
    string _draggingNode;
    Vector2 _dragStartMouse;
    Vector2 _dragStartPos;

    [MenuItem("Window/FSM/State Machine Graph")]
    static void Open()
    {
        GetWindow<StateMachineGraphWindow>("State Machine Graph");
    }

    void OnSelectionChange()
    {
        var go = Selection.activeGameObject;
        if (go == null)
            return;

        var owner = go.GetComponentInParent<IStateMachineOwner>();
        if (owner == null)
            return;

        _owner   = owner;
        _machine = _owner.Machine;

        Repaint();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Target (IStateMachineOwner)", EditorStyles.boldLabel);
        var newOwner = EditorGUILayout.ObjectField("Owner", _owner as Object, typeof(MonoBehaviour), true) as MonoBehaviour;

        if (newOwner != _owner)
        {
            _owner   = newOwner as IStateMachineOwner;
            _machine = _owner?.Machine;
            _nodePositions.Clear();
        }

        if (_owner == null || _machine == null)
        {
            EditorGUILayout.HelpBox("씬에서 IStateMachineOwner를 구현한 컴포넌트를 선택하세요.", MessageType.Info);
            return;
        }

        // 스냅샷
        _machine.GetDebugSnapshot(_stateInfos, _transInfos);

        EditorGUILayout.Space();
        _zoom = EditorGUILayout.Slider("Zoom", _zoom, 0.5f, 2f);

        EditorGUILayout.Space();
        _saveTarget = (StateMachineGraphAsset)EditorGUILayout.ObjectField(
            "Save Asset", _saveTarget, typeof(StateMachineGraphAsset), false);

        // 에셋 좌표 로드 (처음 한 번)
        if (_saveTarget != null && _nodePositions.Count == 0)
        {
            LoadPositionsFromAsset();
        }

        if (GUILayout.Button("Save Snapshot To Asset"))
        {
            SaveToAsset();
        }

        EditorGUILayout.Space();
        DrawGraphArea();
    }

    void DrawGraphArea()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // 캔버스 영역 (그냥 배경용)
        Rect canvasRect = GUILayoutUtility.GetRect(
            position.width, position.height,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        GUI.Box(canvasRect, GUIContent.none);

        const float baseNodeWidth  = 140f;
        const float baseNodeHeight = 40f;
        Vector2 nodeSize = new Vector2(baseNodeWidth, baseNodeHeight) * _zoom;

        // ----------------------------
        // 1) 그래프 분석 (indegree/depth)
        // ----------------------------
        var indegree = new Dictionary<string, int>();
        var children = new Dictionary<string, List<string>>();

        foreach (var s in _stateInfos)
        {
            indegree[s.Name] = 0;
            children[s.Name] = new List<string>();
        }

        foreach (var tr in _transInfos)
        {
            if (tr.From == "Any") // Any는 루트로 취급
                continue;

            if (!indegree.ContainsKey(tr.To))
                indegree[tr.To] = 0;
            if (!children.ContainsKey(tr.From))
                children[tr.From] = new List<string>();

            indegree[tr.To]++;
            children[tr.From].Add(tr.To);
        }

        // 시작 상태 (indegree == 0)
        var startStates = new HashSet<string>();
        foreach (var kv in indegree)
        {
            if (kv.Value == 0)
                startStates.Add(kv.Key);
        }

        var depthByState = ComputeDepthByBfs(_stateInfos, children, startStates);

        // depth 그룹
        var depthGroups = new Dictionary<int, List<string>>();
        foreach (var s in _stateInfos)
        {
            int d = depthByState.TryGetValue(s.Name, out var v) ? v : 0;
            if (!depthGroups.ContainsKey(d))
                depthGroups[d] = new List<string>();
            depthGroups[d].Add(s.Name);
        }

        var sortedDepths = new List<int>(depthGroups.Keys);
        sortedDepths.Sort();

        const float layerSpacingY = 120f;
        const float nodeSpacingX  = 200f;
        const float paddingX      = 80f;
        const float paddingY      = 80f;

        int maxCount = 1;
        foreach (var g in depthGroups.Values)
            if (g.Count > maxCount) maxCount = g.Count;

        // 2) 아직 위치 없는 노드들에 대해 depth 기반 기본 배치
        foreach (var depth in sortedDepths)
        {
            var list = depthGroups[depth];
            int count = list.Count;

            float startX = paddingX + (maxCount - count) * nodeSpacingX * 0.5f;
            float y      = paddingY + depth * layerSpacingY;

            for (int i = 0; i < count; i++)
            {
                string name = list[i];
                if (_nodePositions.ContainsKey(name))
                    continue;

                float x = startX + i * nodeSpacingX;
                _nodePositions[name] = new Vector2(x, y);
            }
        }

        // 현재 활성 상태 이름
        string activeStateName = _machine.CurrentState != null
            ? _machine.CurrentState.GetType().Name
            : null;

        // Any 노드 (있으면 위쪽에 고정)
        bool hasAny = _transInfos.Exists(t => t.From == "Any");
        Rect? anyRect = null;
        if (hasAny)
        {
            // Any 도 드래그 가능하게 하려면 _nodePositions에 넣어 관리해도 됨
            if (!_nodePositions.TryGetValue("Any", out var anyPos))
            {
                anyPos = new Vector2(20f, 20f);
                _nodePositions["Any"] = anyPos;
            }

            Vector2 drawPos = new Vector2(
                canvasRect.x + _nodePositions["Any"].x * _zoom,
                canvasRect.y + _nodePositions["Any"].y * _zoom);

            Rect rect = new Rect(drawPos, nodeSize);
            anyRect = rect;
        }

        var nodeRects = new Dictionary<string, Rect>();

        // ----------------------------
        // 3) 노드 박스 + 활성 강조
        // ----------------------------
        foreach (var info in _stateInfos)
        {
            if (!_nodePositions.TryGetValue(info.Name, out var pos))
                pos = Vector2.zero;

            Vector2 drawPos = new Vector2(
                canvasRect.x + pos.x * _zoom,
                canvasRect.y + pos.y * _zoom);

            var rect = new Rect(drawPos, nodeSize);
            nodeRects[info.Name] = rect;

            // 바깥 박스
            GUI.Box(rect, GUIContent.none);

            // 안쪽 박스
            var innerRect = new Rect(
                rect.x + 4,
                rect.y + 4,
                rect.width  - 8,
                rect.height - 8);

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = info.Name == activeStateName ? FontStyle.Bold : FontStyle.Normal
            };

            GUI.Box(innerRect, GUIContent.none);
            GUI.Label(innerRect, info.Name, labelStyle);

            // 활성 상태 테두리
            if (info.Name == activeStateName)
            {
                Handles.BeginGUI();
                var prevColor = Handles.color;
                Handles.color = Color.yellow;
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Color.yellow);
                Handles.color = prevColor;
                Handles.EndGUI();
            }
        }

        // Any 노드 실제 그리기
        if (anyRect.HasValue)
        {
            var rect = anyRect.Value;

            GUI.Box(rect, GUIContent.none);
            var innerRect = new Rect(
                rect.x + 4,
                rect.y + 4,
                rect.width  - 8,
                rect.height - 8);

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };

            GUI.Box(innerRect, GUIContent.none);
            GUI.Label(innerRect, "Any", style);

            nodeRects["Any"] = rect;
        }

        // 4) 노드 드래그 (좌표는 zoom 이전 기준으로 저장)
        HandleNodeDragging(nodeRects, canvasRect);

        // ----------------------------
        // 5) 전이: 직각 + 화살표
        // ----------------------------
        Handles.BeginGUI();
        var prevCol = Handles.color;

        foreach (var tr in _transInfos)
        {
            if (!nodeRects.TryGetValue(tr.From, out var fromRect))
                continue;
            if (!nodeRects.TryGetValue(tr.To, out var toRect))
                continue;

            Vector3 fromPos = new Vector3(fromRect.xMax, fromRect.center.y, 0f);
            Vector3 toPos   = new Vector3(toRect.xMin, toRect.center.y, 0f);

            // ㄱ자 경로: p0 -> p1 -> p2 -> p3
            float midX = (fromRect.xMax + toRect.xMin) * 0.5f;
            Vector3 p0 = fromPos;
            Vector3 p1 = new Vector3(midX, fromPos.y, 0f);
            Vector3 p2 = new Vector3(midX, toPos.y, 0f);
            Vector3 p3 = toPos;

            Handles.color = Color.white;

            // 앞 2개 세그먼트는 그냥 라인
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p1, p2);

            // 마지막 세그먼트는 화살표
            DrawArrow(p2, p3, 8f * _zoom);

            // 라벨 (중간 코너 부근)
            string core =
                !string.IsNullOrEmpty(tr.Name) && !string.IsNullOrEmpty(tr.ConditionType)
                    ? $"{tr.Name} [{tr.ConditionType}]"
                    : !string.IsNullOrEmpty(tr.Name)
                        ? tr.Name
                        : tr.ConditionType;

            string label = $"{core} (prio {tr.Priority})";

            Vector3 labelPos = (p1 + p2) * 0.5f;
            var size = GUI.skin.label.CalcSize(new GUIContent(label));
            var labelRect = new Rect(
                labelPos.x - size.x * 0.5f,
                labelPos.y - size.y * 0.5f - 10,
                size.x,
                size.y);

            GUI.Label(labelRect, label);
        }

        Handles.color = prevCol;
        Handles.EndGUI();

        EditorGUILayout.EndScrollView();
    }

    // BFS로 depth 계산
    Dictionary<string, int> ComputeDepthByBfs(
        List<StateMachine.StateInfo> states,
        Dictionary<string, List<string>> children,
        HashSet<string> startStates)
    {
        var depth   = new Dictionary<string, int>();
        var visited = new HashSet<string>();
        var q       = new Queue<string>();

        if (startStates.Count > 0)
        {
            foreach (var s in startStates)
            {
                depth[s] = 0;
                visited.Add(s);
                q.Enqueue(s);
            }
        }
        else if (states.Count > 0)
        {
            depth[states[0].Name] = 0;
            visited.Add(states[0].Name);
            q.Enqueue(states[0].Name);
        }

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int curDepth = depth[cur];

            if (!children.TryGetValue(cur, out var list))
                continue;

            foreach (var next in list)
            {
                if (visited.Contains(next))
                    continue;

                visited.Add(next);
                depth[next] = curDepth + 1;
                q.Enqueue(next);
            }
        }

        // BFS에 안 잡힌 노드들은 depth 0
        foreach (var s in states)
        {
            if (!depth.ContainsKey(s.Name))
                depth[s.Name] = 0;
        }

        return depth;
    }

    void HandleNodeDragging(Dictionary<string, Rect> nodeRects, Rect canvasRect)
    {
        var e = Event.current;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    foreach (var kvp in nodeRects)
                    {
                        if (kvp.Value.Contains(e.mousePosition))
                        {
                            _draggingNode   = kvp.Key;
                            _dragStartMouse = e.mousePosition;

                            if (_nodePositions.TryGetValue(_draggingNode, out var basePos))
                                _dragStartPos = basePos;
                            else
                                _dragStartPos = new Vector2(
                                    (kvp.Value.x - canvasRect.x) / _zoom,
                                    (kvp.Value.y - canvasRect.y) / _zoom);

                            e.Use();
                            break;
                        }
                    }
                }
                break;

            case EventType.MouseDrag:
                if (_draggingNode != null && e.button == 0)
                {
                    var delta = (e.mousePosition - _dragStartMouse) / _zoom;
                    _nodePositions[_draggingNode] = _dragStartPos + delta;
                    Repaint();
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0 && _draggingNode != null)
                {
                    _draggingNode = null;
                    e.Use();
                }
                break;
        }
    }

    // 직선 + 화살표
    void DrawArrow(Vector3 from, Vector3 to, float headSize)
    {
        Handles.DrawLine(from, to);

        Vector3 dir  = (to - from).normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

        Vector3 left  = to - dir * headSize + perp * (headSize * 0.5f);
        Vector3 right = to - dir * headSize - perp * (headSize * 0.5f);

        Handles.DrawLine(to, left);
        Handles.DrawLine(to, right);
    }

    void LoadPositionsFromAsset()
    {
        if (_saveTarget == null)
            return;

        _nodePositions.Clear();
        foreach (var n in _saveTarget.nodes)
        {
            _nodePositions[n.stateName] = n.position;
        }
    }

    void SaveToAsset()
    {
        if (_saveTarget == null)
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save FSM Graph",
                "StateMachineGraph",
                "asset",
                "그래프를 저장할 위치를 선택하세요");

            if (string.IsNullOrEmpty(path))
                return;

            _saveTarget = ScriptableObject.CreateInstance<StateMachineGraphAsset>();
            AssetDatabase.CreateAsset(_saveTarget, path);
        }

        _saveTarget.machineName = _owner.GetType().Name;
        _saveTarget.nodes.Clear();
        _saveTarget.transitions.Clear();

        // 현재 좌표 저장 (zoom 전 기준 값)
        foreach (var info in _stateInfos)
        {
            if (!_nodePositions.TryGetValue(info.Name, out var pos))
                pos = Vector2.zero;

            _saveTarget.nodes.Add(new StateMachineGraphAsset.NodeData
            {
                stateName = info.Name,
                position  = pos
            });
        }

        foreach (var tr in _transInfos)
        {
            string core =
                !string.IsNullOrEmpty(tr.Name) && !string.IsNullOrEmpty(tr.ConditionType)
                    ? $"{tr.Name} [{tr.ConditionType}]"
                    : !string.IsNullOrEmpty(tr.Name)
                        ? tr.Name
                        : tr.ConditionType;

            string label = $"{core} (prio {tr.Priority})";

            _saveTarget.transitions.Add(new StateMachineGraphAsset.TransitionData
            {
                fromState = tr.From,
                toState   = tr.To,
                label     = label
            });
        }

        EditorUtility.SetDirty(_saveTarget);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
