#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesGUIColorAttribute))]
    public class AesGUIColorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AesGUIColorAttribute)attribute;
            var target = property.serializedObject.targetObject;

            var prev = GUI.color;
            if (AesGUIColorHelper.TryGetColor(target, attr, out var col))
                GUI.color = col;

            EditorGUI.PropertyField(position, property, label, true);

            GUI.color = prev;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif