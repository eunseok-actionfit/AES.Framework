#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesEnumToggleButtonsAttribute))]
    public class AesEnumToggleButtonsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.HelpBox(position, "[AesEnumToggleButtons] 은 Enum 전용입니다.", MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            EditorGUI.LabelField(labelRect, label);

            Rect buttonRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                position.height
            );

            string[] names = property.enumDisplayNames;
            int index = property.enumValueIndex;

            int newIndex = GUI.Toolbar(buttonRect, index, names);
            if (newIndex != index)
            {
                property.enumValueIndex = newIndex;
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif