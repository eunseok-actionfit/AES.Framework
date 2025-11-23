#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Editor
{
    [CustomEditor(typeof(BindingBehaviour), true)]
    public class BindingBehaviourEditor : UnityEditor.Editor
    {
        static bool _showDebug = true;

        public override void OnInspectorGUI()
        {
            // 먼저 원래 인스펙터 그리기
            base.OnInspectorGUI();

            // 디버그 전역 토글이 꺼져 있으면 그리지 않음
            if (!BindingDebugSettings.Enabled)
                return;

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            _showDebug = EditorGUILayout.Foldout(_showDebug, "Binding Debug Info", false);

            if (_showDebug)
            {
                DrawDebugField("_debugContextName",  "Context");
                DrawDebugField("_debugMemberPath",   "Path");
                DrawDebugField("_debugLastValue",    "Last Value");
                DrawDebugField("_debugLastError",    "Last Error");
                DrawDebugField("_debugUpdateCount",  "Update Count");
            }

            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawDebugField(string propName, string label)
        {
            var prop = serializedObject.FindProperty(propName);
            if (prop == null)
                return;

            using (new EditorGUI.DisabledScope(true))
            {
                if (prop.propertyType == SerializedPropertyType.String)
                    EditorGUILayout.TextField(label, prop.stringValue);
                else if (prop.propertyType == SerializedPropertyType.Integer)
                    EditorGUILayout.IntField(label, prop.intValue);
            }
        }
    }
}
#endif