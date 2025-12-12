#if UNITY_EDITOR
using System.Collections.Generic;
using AES.Tools;
using AES.Tools.Debugging;
using UnityEditor;
using UnityEngine;

public sealed class StateMachineGraphWindow : EditorWindow
{
    IStateMachineOwner _owner;
    StateMachine _machine;

    readonly List<StateMachine.StateInfo> _stateInfos = new();
    readonly List<StateMachine.TransitionInfo> _transInfos = new();

    // 노드 위치 (월드 좌표, zoom 적용 전 기준)
    readonly Dictionary<string, Vector2> _nodePositions = new();

    // 상태별 선 색상
    readonly Dictionary<string, Color> _stateColors = new();

    float _zoom = 1f;
    Vector2 _pan = Vector2.zero; // 월드 좌표 기준 패닝 오프셋

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

    void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnEditorUpdate()
    {
        if (_owner != null && _machine != null)
            Repaint();
    }

    void OnSelectionChange()
    {
        var go = Selection.activeGameObject;
        if (go == null)
            return;

        var owner = go.GetComponentInParent<IStateMachineOwner>();
        if (owner == null)
            return;

        _owner = owner;
        _machine = _owner.Machine;

        Repaint();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Target (IStateMachineOwner)", EditorStyles.boldLabel);
        var newOwner = EditorGUILayout.ObjectField("Owner", _owner as Object, typeof(MonoBehaviour), true) as MonoBehaviour;

        if (newOwner != _owner)
        {
            _owner = newOwner as IStateMachineOwner;
            _machine = _owner?.Machine;
            _nodePositions.Clear();
            _pan = Vector2.zero;
        }

        if (_owner == null || _machine == null)
        {
            EditorGUILayout.HelpBox("씬에서 IStateMachineOwner를 구현한 컴포넌트를 선택하세요.", MessageType.Info);
            return;
        }

        // 스냅샷
        _machine.GetDebugSnapshot(_stateInfos, _transInfos);

        EditorGUILayout.Space();
        _zoom = EditorGUILayout.Slider("Zoom", _zoom, 0.3f, 3f);

        EditorGUILayout.Space();
        _saveTarget = (StateMachineGraphAsset)EditorGUILayout.ObjectField(
            "Save Asset", _saveTarget, typeof(StateMachineGraphAsset), false);

        // 에셋 좌표 로드 (처음 한 번)
        if (_saveTarget != null && _nodePositions.Count == 0) { LoadPositionsFromAsset(); }

        if (GUILayout.Button("Save Snapshot To Asset")) { SaveToAsset(); }

        EditorGUILayout.Space();
        DrawGraphArea();
    }

    void DrawGraphArea()
    {
        // 캔버스 영역 (배경)
        Rect canvasRect = GUILayoutUtility.GetRect(
            position.width, position.height,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        GUI.Box(canvasRect, GUIContent.none);

        // 휠 줌 / 휠 클릭 패닝
        HandlePanAndZoom(canvasRect);

        const float baseNodeWidth = 140f;
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

            indegree.TryAdd(tr.To, 0);

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
            int d = depthByState.GetValueOrDefault(s.Name, 0);
            if (!depthGroups.ContainsKey(d))
                depthGroups[d] = new List<string>();

            depthGroups[d].Add(s.Name);
        }

        var sortedDepths = new List<int>(depthGroups.Keys);
        sortedDepths.Sort();

        // 세로 흐름 기준: depth = Y 레벨
        const float layerSpacingY = 120f;
        const float nodeSpacingX = 200f;
        const float paddingX = 80f;
        const float paddingY = 80f;

        int maxCount = 1;
        foreach (var g in depthGroups.Values)
            if (g.Count > maxCount)
                maxCount = g.Count;

        // 2) 아직 위치 없는 노드들에 대해 depth 기반 기본 배치
        foreach (var depth in sortedDepths)
        {
            var list = depthGroups[depth];
            int count = list.Count;

            float startX = paddingX + (maxCount - count) * nodeSpacingX * 0.5f;
            float y = paddingY + depth * layerSpacingY;

            for (int i = 0; i < count; i++)
            {
                string name = list[i];
                if (_nodePositions.ContainsKey(name))
                    continue;

                float x = startX + i * nodeSpacingX;
                _nodePositions[name] = new Vector2(x, y);
            }
        }

        // ----------------------------
        // FIX 1) 활성 상태 이름 가져오기 버그 수정
        // 기존 코드: _machine.CurrentState.GetType().Name (ObservableProperty 타입)
        // ----------------------------
        string activeStateName = _machine.CurrentStateRaw?.GetType().Name;

        // Any 노드 (있으면 위쪽에 고정)
        bool hasAny = _transInfos.Exists(t => t.From == "Any");
        Rect? anyRect = null;

        if (hasAny)
        {
            if (!_nodePositions.TryGetValue("Any", out var anyPos))
            {
                anyPos = new Vector2(20f, 20f);
                _nodePositions["Any"] = anyPos;
            }

            Vector2 anyWorld = _nodePositions["Any"] + _pan;
            Vector2 drawPos = new Vector2(
                canvasRect.x + anyWorld.x * _zoom,
                canvasRect.y + anyWorld.y * _zoom);

            Rect rect = new Rect(drawPos, nodeSize);
            anyRect = rect;
        }

        var nodeRects = new Dictionary<string, Rect>();

        // ----------------------------
        // 3) 노드 박스 + 활성 색상 변경(배경 채우기)
        // ----------------------------
        foreach (var info in _stateInfos)
        {
            if (!_nodePositions.TryGetValue(info.Name, out var pos))
                pos = Vector2.zero;

            Vector2 worldPos = pos + _pan;

            Vector2 drawPos = new Vector2(
                canvasRect.x + worldPos.x * _zoom,
                canvasRect.y + worldPos.y * _zoom);

            var rect = new Rect(drawPos, nodeSize);
            nodeRects[info.Name] = rect;

            bool isActive = (info.Name == activeStateName);

            // FIX 2) 배경색 실제로 칠하기
            // - 비활성: 옅게 상태 고유색
            // - 활성: 더 진하게 + 노란 테두리
            Color baseCol = GetColorForState(info.Name);
            Color fillCol = isActive
                ? new Color(baseCol.r, baseCol.g, baseCol.b, 0.28f)
                : new Color(baseCol.r, baseCol.g, baseCol.b, 0.10f);

            EditorGUI.DrawRect(rect, fillCol);

            // 프레임/레이아웃은 Box로 유지
            GUI.Box(rect, GUIContent.none);

            var innerRect = new Rect(
                rect.x + 4,
                rect.y + 4,
                rect.width - 8,
                rect.height - 8);

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal
            };

            GUI.Box(innerRect, GUIContent.none);
            GUI.Label(innerRect, info.Name, labelStyle);

            if (isActive)
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

            // Any도 배경 살짝
            EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.06f));

            GUI.Box(rect, GUIContent.none);
            var innerRect = new Rect(
                rect.x + 4,
                rect.y + 4,
                rect.width - 8,
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

        // 4) 노드 드래그 (줌/패닝 고려)
        HandleNodeDragging(nodeRects);

        // ----------------------------
        // 5) 전이: 직각 경로 + 색상
        // ----------------------------
        Handles.BeginGUI();
        var prevCol = Handles.color;

        float portMargin = 4f * _zoom; // 박스 테두리에서 살짝 띄우기
        float extendLength = 30f * _zoom; // 변 방향으로 더 뻗는 길이
        float lineWidth = 2f;
        float laneStep = 4f; // 레인 간 간격(픽셀)

        foreach (var tr in _transInfos)
        {
            if (!nodeRects.TryGetValue(tr.From, out var fromRect))
                continue;

            if (!nodeRects.TryGetValue(tr.To, out var toRect))
                continue;

            Color lineColor = GetColorForState(tr.From);

            // 전이 이름 해시로 레인 결정 (-2 ~ 2)
            int hash = tr.From.GetHashCode() ^ (tr.To.GetHashCode() * 397);
            int laneIndex = Mathf.Abs(hash) % 5 - 2;
            float laneOffset = laneIndex * laneStep;

            ChooseDirectionalPorts(fromRect, toRect, portMargin, laneOffset, extendLength,
                out var fromPort, out var toPort, out int fromSide, out int toSide);

            var path = BuildPathWithSideDirections(
                fromPort, fromSide,
                toPort, toSide,
                extendLength);

            Handles.color = lineColor;

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 p0 = path[i];
                Vector3 p1 = path[i + 1];

                bool isLast = (i == path.Count - 2);

                if (!isLast) { Handles.DrawAAPolyLine(lineWidth, new[] { p0, p1 }); }
                else { DrawArrow(p0, p1, 8f * _zoom, lineWidth, lineColor); }
            }

            string core =
                !string.IsNullOrEmpty(tr.Name) && !string.IsNullOrEmpty(tr.ConditionType)
                    ? $"{tr.Name} [{tr.ConditionType}]"
                    : !string.IsNullOrEmpty(tr.Name)
                        ? tr.Name
                        : tr.ConditionType;

            string label = $"{core} (prio {tr.Priority})";

            Vector3 labelPos;

            if (path.Count >= 2)
            {
                int midIndex = (path.Count - 1) / 2;
                Vector3 a = path[midIndex];
                Vector3 b = path[midIndex + 1];
                labelPos = (a + b) * 0.5f;
            }
            else { labelPos = (fromPort + toPort) * 0.5f; }

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
    }

    // 휠 줌 + 휠 클릭(중클릭) 패닝
    void HandlePanAndZoom(Rect canvasRect)
    {
        var e = Event.current;

        switch (e.type)
        {
            case EventType.ScrollWheel:
            {
                if (!canvasRect.Contains(e.mousePosition))
                    return;

                Vector2 mouse = e.mousePosition;

                // 현재 줌 기준 월드 좌표
                Vector2 worldBefore = (mouse - canvasRect.position) / _zoom - _pan;

                float zoomDelta = 1f - e.delta.y * 0.1f;
                float newZoom = Mathf.Clamp(_zoom * zoomDelta, 0.3f, 3f);

                if (!Mathf.Approximately(newZoom, _zoom))
                {
                    _zoom = newZoom;
                    _pan = (mouse - canvasRect.position) / _zoom - worldBefore;

                    Repaint();
                    e.Use();
                }

                break;
            }

            case EventType.MouseDrag:
            {
                // 중클릭(휠 버튼)으로 패닝
                if (e.button == 2)
                {
                    Vector2 deltaWorld = e.delta / _zoom;
                    _pan += deltaWorld;
                    Repaint();
                    e.Use();
                }

                break;
            }
        }
    }

    // BFS로 depth 계산
    Dictionary<string, int> ComputeDepthByBfs(
        List<StateMachine.StateInfo> states,
        Dictionary<string, List<string>> children,
        HashSet<string> startStates)
    {
        var depth = new Dictionary<string, int>();
        var visited = new HashSet<string>();
        var q = new Queue<string>();

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
                if (!visited.Add(next))
                    continue;

                depth[next] = curDepth + 1;
                q.Enqueue(next);
            }
        }

        foreach (var s in states) { depth.TryAdd(s.Name, 0); }

        return depth;
    }

    // 노드 드래그 (줌/패닝 고려)
    void HandleNodeDragging(Dictionary<string, Rect> nodeRects)
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
                            _draggingNode = kvp.Key;
                            _dragStartMouse = e.mousePosition;

                            if (_nodePositions.TryGetValue(_draggingNode, out var basePos))
                                _dragStartPos = basePos;
                            else
                                _dragStartPos = Vector2.zero;

                            e.Use();
                            break;
                        }
                    }
                }
                break;

            case EventType.MouseDrag:
                if (_draggingNode != null && e.button == 0)
                {
                    var deltaWorld = (e.mousePosition - _dragStartMouse) / _zoom;
                    _nodePositions[_draggingNode] = _dragStartPos + deltaWorld;
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

    // 상태별 고정 색상
    Color GetColorForState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName))
            return Color.white;

        if (_stateColors.TryGetValue(stateName, out var c))
            return c;

        int hash = stateName.GetHashCode();
        float h = (hash & 0x7FFFFFFF) / (float)int.MaxValue;
        float s = 0.6f;
        float v = 0.9f;
        c = Color.HSVToRGB(h, s, v);

        _stateColors[stateName] = c;
        return c;
    }

    // rect 네 면의 중앙
    Vector2 GetSideCenter(Rect r, int sideIndex)
    {
        // 0: Left, 1: Right, 2: Top, 3: Bottom
        switch (sideIndex)
        {
            case 0: return new Vector2(r.xMin, r.center.y);
            case 1: return new Vector2(r.xMax, r.center.y);
            case 2: return new Vector2(r.center.x, r.yMin);
            case 3: return new Vector2(r.center.x, r.yMax);
        }

        return r.center;
    }

    // 각 변의 "밖을 향하는" 법선
    Vector2 GetSideNormal(int sideIndex)
    {
        switch (sideIndex)
        {
            case 0: return new Vector2(-1f, 0f); // Left
            case 1: return new Vector2(1f, 0f); // Right
            case 2: return new Vector2(0f, -1f); // Top
            case 3: return new Vector2(0f, 1f); // Bottom
        }

        return Vector2.zero;
    }

    // 각 변을 따라가는 접선 방향 (레인 오프셋용)
    Vector2 GetSideTangent(int sideIndex)
    {
        switch (sideIndex)
        {
            case 0:
            case 1: return new Vector2(0f, 1f); // 좌/우 변은 위아래로
            case 2:
            case 3: return new Vector2(1f, 0f); // 상/하 변은 좌우로
        }

        return Vector2.zero;
    }

    // fromPort = 박스 밖, toPort = 박스 안쪽
    void ChooseDirectionalPorts(
        Rect fromRect,
        Rect toRect,
        float margin,
        float laneOffset,
        float extendLength,
        out Vector2 fromPort,
        out Vector2 toPort,
        out int fromSide,
        out int toSide)
    {
        Vector2 fromCenter = fromRect.center;
        Vector2 toCenter = toRect.center;
        Vector2 delta = toCenter - fromCenter;

        bool horizontalDominant = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);

        float gapX = Mathf.Abs(delta.x);
        float gapY = Mathf.Abs(delta.y);

        if (horizontalDominant)
        {
            if (gapX < extendLength * 2f)
                horizontalDominant = false;
        }
        else
        {
            if (gapY < extendLength * 2f)
                horizontalDominant = true;
        }

        if (horizontalDominant)
        {
            if (delta.x > 0f)
            {
                fromSide = 1; // Right
                toSide = 0;   // Left
            }
            else
            {
                fromSide = 0; // Left
                toSide = 1;   // Right
            }
        }
        else
        {
            if (delta.y > 0f)
            {
                fromSide = 3; // Bottom
                toSide = 2;   // Top
            }
            else
            {
                fromSide = 2; // Top
                toSide = 3;   // Bottom
            }
        }

        var fCenter = GetSideCenter(fromRect, fromSide);
        var tCenter = GetSideCenter(toRect, toSide);

        Vector2 fTan = GetSideTangent(fromSide).normalized;
        Vector2 tTan = GetSideTangent(toSide).normalized;
        fCenter += fTan * laneOffset;
        tCenter += tTan * laneOffset;

        var fN = GetSideNormal(fromSide);
        var tN = GetSideNormal(toSide);

        fromPort = fCenter + fN * margin;
        toPort = tCenter + tN * margin;
    }

    List<Vector3> BuildPathWithSideDirections(
        Vector2 fromPort,
        int fromSide,
        Vector2 toPort,
        int toSide,
        float extendLength)
    {
        var list = new List<Vector3>();

        Vector2 fromNormal = GetSideNormal(fromSide);
        Vector2 toNormal = GetSideNormal(toSide);

        bool horizontal =
            (fromSide == 0 || fromSide == 1) &&
            (toSide == 0 || toSide == 1);

        float axisDist = horizontal
            ? Mathf.Abs(toPort.x - fromPort.x)
            : Mathf.Abs(toPort.y - fromPort.y);

        if (axisDist < 0.001f)
            axisDist = 0f;

        float usedExtend = Mathf.Min(extendLength, axisDist * 0.5f);

        Vector3 p0 = fromPort;
        Vector3 p1 = p0 + (Vector3)(fromNormal * usedExtend);

        Vector3 p3 = toPort;
        Vector3 p2 = p3 + (Vector3)(toNormal * usedExtend);

        list.Add(p0);
        list.Add(p1);

        if (!Approximately(p1.x, p2.x) && !Approximately(p1.y, p2.y))
        {
            Vector3 mid1 = new Vector3(p2.x, p1.y, 0f);
            Vector3 mid2 = new Vector3(p1.x, p2.y, 0f);

            float len1 = Vector3.Distance(p1, mid1) + Vector3.Distance(mid1, p2);
            float len2 = Vector3.Distance(p1, mid2) + Vector3.Distance(mid2, p2);

            if (len1 <= len2)
                list.Add(mid1);
            else
                list.Add(mid2);
        }

        list.Add(p2);
        list.Add(p3);

        RemoveRedundantPoints(list);
        return list;
    }

    bool Approximately(float a, float b) => Mathf.Abs(a - b) < 0.001f;

    void RemoveRedundantPoints(List<Vector3> pts)
    {
        if (pts.Count <= 1)
            return;

        for (int i = pts.Count - 1; i > 0; i--)
        {
            if (Vector3.Distance(pts[i], pts[i - 1]) < 0.01f)
                pts.RemoveAt(i);
        }
    }

    void DrawArrow(Vector3 from, Vector3 to, float headSize, float lineWidth, Color color)
    {
        Handles.color = color;

        Handles.DrawAAPolyLine(lineWidth, new[] { from, to });

        Vector3 dir = (to - from).normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

        Vector3 left = to - dir * headSize + perp * (headSize * 0.5f);
        Vector3 right = to - dir * headSize - perp * (headSize * 0.5f);

        Handles.DrawAAPolyLine(lineWidth, new[] { to, left });
        Handles.DrawAAPolyLine(lineWidth, new[] { to, right });
    }

    void LoadPositionsFromAsset()
    {
        if (_saveTarget == null)
            return;

        _nodePositions.Clear();

        foreach (var n in _saveTarget.nodes) { _nodePositions[n.stateName] = n.position; }
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

        foreach (var info in _stateInfos)
        {
            if (!_nodePositions.TryGetValue(info.Name, out var pos))
                pos = Vector2.zero;

            _saveTarget.nodes.Add(new StateMachineGraphAsset.NodeData
            {
                stateName = info.Name,
                position = pos
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
                toState = tr.To,
                label = label
            });
        }

        EditorUtility.SetDirty(_saveTarget);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
