#if UNITY_EDITOR
using UnityEditor;


namespace AES.Tools.Editor.DataBinding
{
    [CustomEditor(typeof(BindingBehaviour), true)]
    public class BindingBehaviourEditor : UnityEditor.Editor
    {
        // 기본값을 true → false 로 변경 (처음엔 닫혀있게)
        static bool _showDebug = false;

        public override void OnInspectorGUI()
        {
            // 기본 인스펙터
            base.OnInspectorGUI();

            // 전역 디버그 토글이 꺼져 있으면 아무것도 안 그림
            if (!BindingDebugSettings.Enabled)
                return;

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");

            // Foldout 헤더
            _showDebug = EditorGUILayout.Foldout(
                _showDebug,
                "Binding Debug Info",
                true); // expandedDefaultOnFirstDraw는 true로 두고, 실제 상태는 static bool에 의해 제어

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