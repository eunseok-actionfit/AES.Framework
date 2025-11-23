#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Editor
{
    public class BindingDebuggerWindow : EditorWindow
    {
        [MenuItem("AES/DataBinding/Binding Debugger Window")]
        static void Open()
        {
            GetWindow<BindingDebuggerWindow>("Binding Debugger");
        }

        Vector2 _scroll;

        void OnGUI()
        {
            if (GUILayout.Button("Refresh"))
                Repaint();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            var bindings = FindObjectsByType<BindingBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var b in bindings)
            {
                if (b == null) continue;

                var so = new SerializedObject(b);
                var ctxName = so.FindProperty("_debugContextName")?.stringValue ?? "(unknown)";
                var path    = so.FindProperty("_debugMemberPath")?.stringValue ?? "";
                var val     = so.FindProperty("_debugLastValue")?.stringValue ?? "";
                var err     = so.FindProperty("_debugLastError")?.stringValue ?? "";
                var count   = so.FindProperty("_debugUpdateCount")?.intValue ?? 0;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.ObjectField("Binding", b, typeof(BindingBehaviour), true);
                EditorGUILayout.LabelField("Context", ctxName);
                EditorGUILayout.LabelField("Path", path);
                EditorGUILayout.LabelField("Last Value", val);
                EditorGUILayout.LabelField("Update Count", count.ToString());

                if (!string.IsNullOrEmpty(err))
                {
                    var prev = GUI.color;
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("Error", err, EditorStyles.wordWrappedLabel);
                    GUI.color = prev;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif