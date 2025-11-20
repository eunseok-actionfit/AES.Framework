#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;


namespace AES.Tools.Editor
{
    public sealed class UIRegistryEntryOdinDrawer : OdinValueDrawer<UIRegistryEntry>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (label != null)
                SirenixEditorGUI.Title(label.text, null, TextAlignment.Left, true);

            // 고유 키 생성
            int instId = 0;
            if (Property.Tree.WeakTargets.Count > 0 && Property.Tree.WeakTargets[0] is Object o && o)
                instId = o.GetInstanceID();

            var key = $"ui_registry_tabs::{instId}::{Property.Path}";

            var tabGroup = SirenixEditorGUI.CreateAnimatedTabGroup(key);
            var tabSource = tabGroup.RegisterTab("Source");
            var tabPlacement = tabGroup.RegisterTab("Placement");
            var tabLifetime = tabGroup.RegisterTab("Lifetime");
            var tabOptimization = tabGroup.RegisterTab("Optimization");

            tabGroup.BeginGroup(drawToolbar: true);
            {
                if (tabSource.BeginPage())
                {
                    Draw_Source();
                }
                tabSource.EndPage();

                if (tabPlacement.BeginPage())
                {
                    Draw_Placement();
                }
                tabPlacement.EndPage();

                if (tabLifetime.BeginPage())
                {
                    Draw_Lifetime();
                }
                tabLifetime.EndPage();

                if (tabOptimization.BeginPage())
                {
                    Draw_Optimization();
                }
                tabOptimization.EndPage();
            }
            tabGroup.EndGroup();

        }

        // ===== Helpers =====
        InspectorProperty Child(string name)
            => Property.FindChild(p => p.Name == name, true);

        // ===== Sections =====
        void Draw_Source()
        {
            using (new GUILayout.VerticalScope(SirenixGUIStyles.BoxContainer))
            {
                var prefabProp = Child("prefab") ?? Child("Prefab");
                var guidProp = Child("addressGuid") ?? Child("AddressGuid");

                var prefab = (GameObject)prefabProp?.ValueEntry.WeakSmartValue;
                var guid = guidProp != null ? (string)guidProp.ValueEntry.WeakSmartValue : string.Empty;

                GUIHelper.PushColor(!string.IsNullOrEmpty(guid) ? new Color(0.4f, 0.7f, 1f) : Color.white);
                var newPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
                GUIHelper.PopColor();

                if (prefabProp != null && !Equals(newPrefab, prefab))
                    prefabProp.ValueEntry.WeakSmartValue = newPrefab;

                if (newPrefab == null)
                    SirenixEditorGUI.ErrorMessageBox("Assign a Prefab.");

                var newGuid = GetGuidIfAddressable(newPrefab) ?? string.Empty;
                if (guidProp != null && newGuid != guid)
                    guidProp.ValueEntry.WeakSmartValue = newGuid;

                using (new GUILayout.HorizontalScope())
                {
                    GUI.enabled = newPrefab != null;
                    if (GUILayout.Button("Ping Prefab"))
                        EditorGUIUtility.PingObject(newPrefab);

                    GUI.enabled = !string.IsNullOrEmpty(newGuid);
                    if (GUILayout.Button("Copy GUID"))
                        EditorGUIUtility.systemCopyBuffer = newGuid;

                    GUI.enabled = true;
                }
            }
        }

        void Draw_Placement()
        {
            using (new GUILayout.VerticalScope(SirenixGUIStyles.BoxContainer))
            {
                var scope = Child("Scope");
                var kind = Child("Kind");

                if (scope != null) scope.Draw();
                else EditorGUILayout.LabelField("Layer Scope", "Not found");

                if (kind != null) kind.Draw();
                else EditorGUILayout.LabelField("Layer Kind", "Not found");
            }
        }

        void Draw_Lifetime()
        {
            using (new GUILayout.VerticalScope(SirenixGUIStyles.BoxContainer))
            {
                var conc = Child("Concurrency");
                var excl = Child("ExclusiveGroup");

                if (conc != null) conc.Draw();
                else EditorGUILayout.LabelField("Concurrency", "Not found");

                if (excl != null) excl.Draw();
                else EditorGUILayout.LabelField("Exclusive", "Not found");
            }
        }

        void Draw_Optimization()
        {
            using (new GUILayout.VerticalScope(SirenixGUIStyles.BoxContainer))
            {
                var use = Child("UsePool") ?? Child("usePool");
                var cap = Child("Capacity") ?? Child("capacity");
                var warm = Child("WarmUp") ?? Child("warmUp");
                var ret = Child("ReturnDelay") ?? Child("returnDelay");

                if (use != null) use.Draw();
                else EditorGUILayout.LabelField("Use Pool", "Not found");

                bool on = use != null && use.ValueEntry.WeakSmartValue is bool and true;

                if (on)
                {
                    EditorGUI.indentLevel++;
                    if (cap != null) cap.Draw();
                    if (warm != null) warm.Draw();
                    if (ret != null) ret.Draw();

                    if (GUILayout.Button("Normalize"))
                    {
                        if (cap?.ValueEntry.WeakSmartValue is int c) cap.ValueEntry.WeakSmartValue = Mathf.Max(0, c);
                        int capacity = cap?.ValueEntry.WeakSmartValue is int c2 ? c2 : 0;
                        if (warm?.ValueEntry.WeakSmartValue is int w) warm.ValueEntry.WeakSmartValue = Mathf.Clamp(w, 0, capacity);
                        if (ret?.ValueEntry.WeakSmartValue is float r) ret.ValueEntry.WeakSmartValue = Mathf.Max(0f, r);
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        static string GetGuidIfAddressable(Object obj)
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