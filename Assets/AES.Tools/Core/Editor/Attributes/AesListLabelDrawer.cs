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
            // 컬렉션이 아니면 그냥 기본 필드 그리기
            if (!IsCollection(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var expr = ((AesListLabelAttribute)attribute).Expression;
            var list = GetList(property, label, expr);
            list.DoList(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // 컬렉션이 아니면 기본 높이
            if (!IsCollection(property))
                return EditorGUI.GetPropertyHeight(property, label, true);

            var expr = ((AesListLabelAttribute)attribute).Expression;
            var list = GetList(property, label, expr);
            return list.GetHeight();
        }

        private bool IsCollection(SerializedProperty property)
        {
            // Unity 배열과 List<T> 모두 isArray == true 이고 Generic 타입
            return property.isArray && property.propertyType == SerializedPropertyType.Generic;
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
                return EditorGUI.GetPropertyHeight(element, true) + 4f;
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

        private string EvaluateExpression(SerializedProperty element, string expr)
        {
            if (string.IsNullOrEmpty(expr))
                return element.displayName;

            // "@environment + \" / \" + platform" -> "environment + \" / \" + platform"
            if (expr.StartsWith("@"))
                expr = expr.Substring(1);

            // 1) element 하위 전체를 한 번 돌면서 "필드이름 → 문자열 값" 맵을 만든다.
            var valueMap = new Dictionary<string, string>();

            var copy = element.Copy();
            var end = element.GetEndProperty();
            bool enterChildren = true;

            while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                enterChildren = false;

                // 동일 이름이 여러 번 나올 수 있지만, AdsProfile 구조에선 충돌 안 난다고 가정
                valueMap[copy.name] = PropertyToString(copy);
            }

            // 2) 표현식 파싱해서 + 기준으로 이어 붙이기
            string output = "";
            var parts = expr.Split('+');

            foreach (var raw in parts)
            {
                var s = raw.Trim();
                if (s.Length == 0)
                    continue;

                // "리터럴 문자열"
                if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
                {
                    output += s.Substring(1, s.Length - 2);
                    continue;
                }

                // 필드/프로퍼티 이름 → valueMap에서 가져오기
                if (valueMap.TryGetValue(s, out var v))
                    output += v;
            }

            return string.IsNullOrEmpty(output) ? element.displayName : output;
        }


        // root 하위 전체를 순회하면서 name이 일치하는 SerializedProperty 탐색
        private SerializedProperty FindRelativeRecursive(SerializedProperty root, string name)
        {
            var copy = root.Copy();
            var end = root.GetEndProperty();

            bool enterChildren = true;
            while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                enterChildren = false;

                if (copy.name == name)
                    return copy.Copy();
            }

            return null;
        }

        private string PropertyToString(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.String:
                    return p.stringValue;

                case SerializedPropertyType.Enum:
                    if (p.enumValueIndex >= 0 && p.enumValueIndex < p.enumDisplayNames.Length)
                        return p.enumDisplayNames[p.enumValueIndex];
                    return p.enumDisplayNames.Length > 0 ? p.enumDisplayNames[0] : p.displayName;

                case SerializedPropertyType.Integer:
                    return p.intValue.ToString();

                case SerializedPropertyType.Boolean:
                    return p.boolValue ? "True" : "False";

                case SerializedPropertyType.Float:
                    return p.floatValue.ToString();

                case SerializedPropertyType.ObjectReference:
                    return p.objectReferenceValue != null ? p.objectReferenceValue.name : "None";

                default:
                    // 복잡한 타입은 이름만 표시 (Odin의 WeakSmartValue.ToString()과 완전 동일하게는 불가)
                    return p.displayName;
            }
        }
    }
}
#endif
