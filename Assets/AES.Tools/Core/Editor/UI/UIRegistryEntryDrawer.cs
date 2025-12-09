#if UNITY_EDITOR && !ODIN_INSPECTOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;


namespace AES.Tools.Editor.UI
{
    [CustomPropertyDrawer(typeof(UIRegistryEntry))]
    public sealed class UIRegistryEntryDrawer : PropertyDrawer
    {
        static readonly GUIContent GC_Source = new("Source");
        //static readonly GUIContent GC_Placement = new("Placement");
        //static readonly GUIContent GC_Lifetime = new("Lifetime");
        static readonly GUIContent GC_Optimization = new("Optimization");

        static readonly string[] Tabs = { "Source",/*"Placement", "Lifetime",*/ "Optimization" };
        static readonly Dictionary<string, int> _tabState = new();
        static readonly Dictionary<string, bool> _expandState = new();

        const float Pad = 2f;
        const float SectionGap = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var expanded = IsExpanded(property);

            if (!expanded)
            {
                float h = EditorGUIUtility.singleLineHeight * 3 + (Pad + SectionGap); // header(2) + prefab(1)
                if (property.FindPropertyRelative("prefab").objectReferenceValue == null)
                    h += EditorGUIUtility.singleLineHeight * 2 + 12;

                return h;
            }

            float hh = EditorGUIUtility.singleLineHeight * 3 + (Pad + SectionGap);

            switch (CurrentTab(property))
            {
                case 0: hh += EditorGUIUtility.singleLineHeight * 3 + 10; break; // Source
                case 1: hh += EditorGUIUtility.singleLineHeight * 3 + 10; break; // Placement: Scope + Kind
                case 2: hh += EditorGUIUtility.singleLineHeight * 3 + 12; break; // Lifetime
                case 3:
                {
                    float line = EditorGUIUtility.singleLineHeight;

                    var use = property.FindPropertyRelative("UsePool")
                              ?? property.FindPropertyRelative("usePool");

                    var cap = property.FindPropertyRelative("Capacity")
                              ?? property.FindPropertyRelative("capacity");

                    var warm = property.FindPropertyRelative("WarmUp")
                               ?? property.FindPropertyRelative("warmUp");

                    var ret = property.FindPropertyRelative("ReturnDelay")
                              ?? property.FindPropertyRelative("returnDelay");

                    // UsePool 토글 한 줄
                    hh += line + Pad;

                    // UsePool == true 일 때만 상세 필드 높이 반영
                    if (use != null && use.boolValue)
                    {
                        if (cap != null) hh += EditorGUI.GetPropertyHeight(cap, true) + Pad;
                        if (warm != null) hh += EditorGUI.GetPropertyHeight(warm, true) + Pad;
                        if (ret != null) hh += EditorGUI.GetPropertyHeight(ret, true) + Pad;

                        // Normalize 버튼 한 줄
                        hh += line + Pad;
                    }

                    break;
                }
            }

            if (CurrentTab(property) == 0 && property.FindPropertyRelative("prefab").objectReferenceValue == null)
                hh += EditorGUIUtility.singleLineHeight * 2 + 12;

            return hh;
        }

        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, property);
            var r = pos;

            // Foldout
            var foldR = new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight);
            var expanded = EditorGUI.Foldout(foldR, IsExpanded(property), label, true);
            SetExpanded(property, expanded);

            // Summary line
            r.y += EditorGUIUtility.singleLineHeight + Pad;
            var kindProp = FindEnum(property, "Kind");
            var scopeProp = FindEnum(property, "Scope");
            var summary = $"Kind={SafeEnumName(kindProp)} | Scope={SafeEnumName(scopeProp)}";
            EditorGUI.LabelField(new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight), summary, EditorStyles.miniLabel);
            r.y += EditorGUIUtility.singleLineHeight + SectionGap;

            if (!expanded)
            {
                Draw_CompactBody(ref r, property);
                EditorGUI.EndProperty();
                return;
            }

            // Tabs
            var tabsR = new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight);
            var tab = GUI.Toolbar(tabsR, CurrentTab(property), Tabs);
            SetTab(property, tab);
            r.y += tabsR.height + SectionGap;

            var body = new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight);

            switch (tab)
            {
                case 0: Draw_Source(ref body, property); break;
             //   case 1: Draw_Placement(ref body, property); break;
               // case 2: Draw_Lifetime(ref body, property); break;
                case 1: Draw_Optimization(ref body, property); break;
            }

            EditorGUI.EndProperty();
        }

        // 접힘 상태: 최소 본문
        void Draw_CompactBody(ref Rect r, SerializedProperty p)
        {
            var prefabProp = p.FindPropertyRelative("prefab");
            var prefabObj = prefabProp.objectReferenceValue;

            if (prefabObj == null)
            {
                var help = new Rect(r.x, r.y + 4, r.width, EditorGUIUtility.singleLineHeight * 2);
                EditorGUI.HelpBox(help, "Assign a Prefab.", MessageType.Warning);
                r.y += help.height + 4;
            }

            p.serializedObject.ApplyModifiedProperties();
        }

        // ===== Sections =====
        void Draw_Source(ref Rect r, SerializedProperty p)
        {
            var prefabProp = p.FindPropertyRelative("prefab");
            var guidProp = p.FindPropertyRelative("addressGuid");

            var row0 = NextRow(ref r);
            var prev = GUI.color;
            var prefabObj = prefabProp.objectReferenceValue;
            var guid = GetGuidIfAddressable(prefabObj);
            var isAddr = !string.IsNullOrEmpty(guid);
            GUI.color = isAddr ? new Color(0.4f, 0.7f, 1f) : Color.white;
            EditorGUI.PropertyField(row0, prefabProp, new GUIContent("Prefab"));
            GUI.color = prev;

            if (guidProp != null) guidProp.stringValue = guid ?? string.Empty;

            var row1 = NextRow(ref r);
            var leftW = row1.width * 0.6f;
            var right = new Rect(row1.x + leftW + 4, row1.y, row1.width - leftW - 4, row1.height);

            using (new EditorGUI.DisabledScope(prefabObj == null))
                if (GUI.Button(new Rect(right.x, right.y, right.width / 2f - 2, right.height), "Ping Prefab"))
                    EditorGUIUtility.PingObject(prefabObj);

            if (GUI.Button(new Rect(right.x + right.width / 2f + 2, right.y, right.width / 2f - 2, right.height), "Copy GUID"))
                if (!string.IsNullOrEmpty(guid))
                    EditorGUIUtility.systemCopyBuffer = guid;

            if (prefabObj == null)
            {
                var help = new Rect(r.x, r.y + 4, r.width, EditorGUIUtility.singleLineHeight * 2);
                EditorGUI.HelpBox(help, "Assign a Prefab.", MessageType.Warning);
                r.y += help.height + 4;
            }

            p.serializedObject.ApplyModifiedProperties();
        }

        void Draw_Placement(ref Rect r, SerializedProperty p)
        {
            var scope = FindEnum(p, "Scope");
            var kind = FindEnum(p, "Kind");

            // 헤더
            // var head = NextRow(ref r);
            // EditorGUI.LabelField(head, GC_Placement, EditorStyles.boldLabel);
            //  r.y += SectionGap - Pad;

            // Scope
            var row1 = NextRow(ref r);
            if (scope != null && scope.propertyType == SerializedPropertyType.Enum)
                EditorGUI.PropertyField(row1, scope, new GUIContent("Layer Scope"));
            else
                EditorGUI.LabelField(row1, "Layer Scope", "Not found");

            // Kind
            var row2 = NextRow(ref r);
            if (kind != null && kind.propertyType == SerializedPropertyType.Enum)
                EditorGUI.PropertyField(row2, kind, new GUIContent("Layer Kind"));
            else
                EditorGUI.LabelField(row2, "Layer Kind", "Not found");
        }

        void Draw_Lifetime(ref Rect r, SerializedProperty p)
        {
            var inst = p.FindPropertyRelative("InstancePolicy");
            var conc = p.FindPropertyRelative("Concurrency");
            var excl = p.FindPropertyRelative("ExclusiveGroup");

            RowEnum(ref r, GUIContent.none, "Instance Policy", inst);
            RowEnum(ref r, GUIContent.none, "Concurrency", conc);
            RowEnum(ref r, GUIContent.none, "Exclusive", excl);
        }

        void Draw_Optimization(ref Rect r, SerializedProperty p)
        {
            var use = p.FindPropertyRelative("UsePool")
                      ?? p.FindPropertyRelative("usePool");

            var cap = p.FindPropertyRelative("Capacity")
                      ?? p.FindPropertyRelative("capacity");

            var warm = p.FindPropertyRelative("WarmUp")
                       ?? p.FindPropertyRelative("warmUp");

            var ret = p.FindPropertyRelative("ReturnDelay")
                      ?? p.FindPropertyRelative("returnDelay");

            // UsePool 토글
            var rowUse = NextRow(ref r);
            EditorGUI.PropertyField(rowUse, use, new GUIContent("Use Pool"));

            // 상세 옵션은 UsePool == true 일 때만
            if (use != null && use.boolValue)
            {
                EditorGUI.indentLevel++;

                if (cap != null)
                {
                    var rc = new Rect(r.x, r.y, r.width, EditorGUI.GetPropertyHeight(cap, true));
                    EditorGUI.PropertyField(rc, cap, new GUIContent("Capacity"), true);
                    r.y += rc.height + Pad;
                }

                if (warm != null)
                {
                    var rw = new Rect(r.x, r.y, r.width, EditorGUI.GetPropertyHeight(warm, true));
                    EditorGUI.PropertyField(rw, warm, new GUIContent("Warm Up"), true);
                    r.y += rw.height + Pad;
                }

                if (ret != null)
                {
                    var rr = new Rect(r.x, r.y, r.width, EditorGUI.GetPropertyHeight(ret, true));
                    EditorGUI.PropertyField(rr, ret, new GUIContent("Return Delay"), true);
                    r.y += rr.height + Pad;
                }

                // Normalize
                var btn = NextRow(ref r);

                if (GUI.Button(btn, "Normalize"))
                {
                    // 제약: Capacity >= 0, 0 <= WarmUp <= Capacity, ReturnDelay >= 0
                    if (cap != null) cap.intValue = Mathf.Max(0, cap.intValue);
                    if (warm != null) warm.intValue = Mathf.Clamp(warm.intValue, 0, (cap != null ? cap.intValue : warm.intValue));
                    if (ret != null) ret.floatValue = Mathf.Max(0f, ret.floatValue);
                    p.serializedObject.ApplyModifiedProperties();
                }

                EditorGUI.indentLevel--;
            }
        }


        // ===== Utils =====
        Rect NextRow(ref Rect r)
        {
            var h = EditorGUIUtility.singleLineHeight;
            var row = new Rect(r.x, r.y, r.width, h);
            r.y += h + Pad;
            return row;
        }

        void RowEnum(ref Rect r, GUIContent header, string name, SerializedProperty enumProp)
        {
            if (header != GUIContent.none)
            {
                var head = NextRow(ref r);
                EditorGUI.LabelField(head, header, EditorStyles.boldLabel);
                r.y += SectionGap - Pad;
            }

            var row = NextRow(ref r);
            if (enumProp != null && enumProp.propertyType == SerializedPropertyType.Enum)
                EditorGUI.PropertyField(row, enumProp, new GUIContent(name));
            else
                EditorGUI.LabelField(row, name, "Not found");
        }

        static bool IsExpanded(SerializedProperty p)
        {
            var key = p.propertyPath;
            return _expandState.TryGetValue(key, out var v) ? v : false;
        }

        static void SetExpanded(SerializedProperty p, bool v) => _expandState[p.propertyPath] = v;

        static string SafeEnumName(SerializedProperty enumProp)
        {
            if (enumProp == null) return "?";
            var names = enumProp.enumDisplayNames;
            var idx = Mathf.Clamp(enumProp.enumValueIndex, 0, Mathf.Max(0, names.Length - 1));
            return names.Length > 0 ? names[idx] : enumProp.enumValueIndex.ToString();
        }

        static int CurrentTab(SerializedProperty p)
        {
            var key = p.propertyPath;
            return _tabState.TryGetValue(key, out var t) ? t : 0;
        }

        static void SetTab(SerializedProperty p, int v) => _tabState[p.propertyPath] = v;

        static SerializedProperty FindEnum(SerializedProperty root, string name)
        {
            // 안전 탐색: 대소문자 혼동 대비
            var p = root.FindPropertyRelative(name);
            if (p == null) p = root.FindPropertyRelative(char.ToLowerInvariant(name[0]) + name.Substring(1));
            if (p == null) p = root.FindPropertyRelative(char.ToUpperInvariant(name[0]) + name.Substring(1));
            return p;
        }

        void NormalizePoolingInline(SerializedProperty pool)
        {
            if (pool == null) return;
            var cap = pool.FindPropertyRelative("Capacity");
            var warm = pool.FindPropertyRelative("WarmUp");
            var ret = pool.FindPropertyRelative("ReturnDelay");

            if (cap != null) cap.intValue = Math.Max(1, cap.intValue);
            if (warm != null) warm.intValue = Mathf.Clamp(warm.intValue, 0, cap?.intValue ?? 8);
            if (ret != null) ret.floatValue = Mathf.Max(0f, ret.floatValue);

            pool.serializedObject.ApplyModifiedProperties();
        }

        // Addressables helpers
        static string GetGuidIfAddressable(UnityEngine.Object obj)
        {
            if (!obj) return null;
            var path = AssetDatabase.GetAssetPath(obj);
            var guid = AssetDatabase.AssetPathToGUID(path);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            return settings && settings.FindAssetEntry(guid) != null ? guid : null;
        }

    }
}
#endif