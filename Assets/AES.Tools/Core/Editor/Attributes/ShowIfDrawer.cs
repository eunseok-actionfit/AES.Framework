using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ShowIfAttribute)attribute;
            var conditionProp = property.serializedObject.FindProperty(attr.ConditionFieldName);

            bool show = conditionProp is { boolValue: true };
            if (show)
                EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (ShowIfAttribute)attribute;
            var conditionProp = property.serializedObject.FindProperty(attr.ConditionFieldName);

            bool show = conditionProp != null && conditionProp.boolValue;
            return show ? EditorGUI.GetPropertyHeight(property, label, true) : 0f;
        }
    }
}