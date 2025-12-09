#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesInlineEditorAttribute))]
    public class AesInlineEditorDrawer : PropertyDrawer
    {
        private UnityEditor.Editor _inlineEditor;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AesInlineEditorAttribute)attribute;

            EditorGUI.PropertyField(position, property, label, true);

            Object obj = property.objectReferenceValue;
            if (obj == null)
                return;

            if (_inlineEditor == null || _inlineEditor.target != obj)
                UnityEditor.Editor.CreateCachedEditor(obj, null, ref _inlineEditor);

            if (_inlineEditor == null)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (attr.DrawHeader)
                EditorGUILayout.LabelField(obj.name, EditorStyles.boldLabel);

            if (attr.DrawPreview && _inlineEditor.HasPreviewGUI())
            {
                float h = Mathf.Max(64f, EditorGUIUtility.singleLineHeight * 4f);
                Rect r = GUILayoutUtility.GetRect(0, h, GUILayout.ExpandWidth(true));
                _inlineEditor.OnPreviewGUI(r, EditorStyles.helpBox);
            }

            _inlineEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif