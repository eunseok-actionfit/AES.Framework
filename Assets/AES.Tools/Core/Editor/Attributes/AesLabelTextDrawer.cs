#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesLabelTextAttribute))]
    public class AesLabelTextDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AesLabelTextAttribute)attribute;
            var custom = string.IsNullOrEmpty(attr.Text)
                ? label
                : new GUIContent(attr.Text, label.tooltip);

            EditorGUI.PropertyField(position, property, custom, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif