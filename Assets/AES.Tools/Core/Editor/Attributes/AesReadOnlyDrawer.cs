#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesReadOnlyAttribute))]
    public class AesReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool prev = GUI.enabled;
            GUI.enabled = false;

            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                // 배열 헤더를 직접 그리되 버튼 제외
                position = EditorGUI.PrefixLabel(position, label);

                int depth = property.depth;
                SerializedProperty iterator = property.Copy();
                SerializedProperty end = iterator.GetEndProperty();

                position.x += 15;
                position.width -= 15;

                while (iterator.NextVisible(true) &&
                       iterator.propertyPath.StartsWith(property.propertyPath) &&
                       iterator.depth > depth)
                {
                    float h = EditorGUI.GetPropertyHeight(iterator, true);
                    EditorGUI.PropertyField(position, iterator, true);
                    position.y += h + 2;
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            GUI.enabled = prev;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif