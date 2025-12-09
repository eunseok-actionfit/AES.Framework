#if UNITY_EDITOR && !ODIN_INSPECTOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesListLabelAttribute))]
    public class AesListLabelDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, ReorderableList> _lists
            = new Dictionary<string, ReorderableList>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var list = GetList(property, label, ((AesListLabelAttribute)attribute).Expression);
            list.DoList(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var list = GetList(property, label, ((AesListLabelAttribute)attribute).Expression);
            return list.GetHeight();
        }

        private ReorderableList GetList(SerializedProperty property, GUIContent label, string expression)
        {
            string key = property.serializedObject.targetObject.GetInstanceID() + property.propertyPath;

            if (_lists.TryGetValue(key, out var list))
                return list;

            list = new ReorderableList(property.serializedObject, property, true, true, true, true);

            list.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, label);
            };

            list.elementHeightCallback = index =>
            {
                var element = property.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 4;
            };

            list.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = property.GetArrayElementAtIndex(index);

                string labelText = EvaluateExpression(element, expression);

                EditorGUI.PropertyField(rect, element, new GUIContent(labelText), true);
            };

            _lists[key] = list;
            return list;
        }

        // 간단한 필드명 기반 표현식 평가기 (ex: "@environment + \" / \" + platform")
        private string EvaluateExpression(SerializedProperty element, string expr)
        {
            string result = expr;

            if (expr.StartsWith("@"))
                expr = expr.Substring(1);

            var parts = expr.Split('+');

            string output = "";

            foreach (var raw in parts)
            {
                string s = raw.Trim();

                // 문자열 리터럴 "xxx"
                if (s.StartsWith("\"") && s.EndsWith("\""))
                {
                    output += s.Substring(1, s.Length - 2);
                    continue;
                }

                // 필드명일 경우
                var p = element.FindPropertyRelative(s);
                if (p != null)
                {
                    output += PropertyToString(p);
                }
            }

            return output == "" ? element.displayName : output;
        }

        private string PropertyToString(SerializedProperty p)
        {
            return p.propertyType switch
            {
                SerializedPropertyType.String => p.stringValue,
                SerializedPropertyType.Enum   => p.enumDisplayNames[p.enumValueIndex],
                SerializedPropertyType.Integer => p.intValue.ToString(),
                SerializedPropertyType.Boolean => p.boolValue.ToString(),
                SerializedPropertyType.Float   => p.floatValue.ToString(),
                _ => p.displayName
            };
        }
    }
}
#endif
