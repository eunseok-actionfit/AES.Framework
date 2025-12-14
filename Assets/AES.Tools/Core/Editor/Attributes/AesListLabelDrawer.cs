#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesListLabelAttribute))]
    public sealed class AesListLabelDrawer : PropertyDrawer
    {
        private readonly Dictionary<string, ReorderableList> _cache = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!IsList(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            var list = GetList(property, label);
            list.DoList(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!IsList(property))
                return EditorGUI.GetPropertyHeight(property, label, true);

            return GetList(property, label).GetHeight();
        }

        // ------------------------

        private bool IsList(SerializedProperty p) =>
            p.isArray && p.propertyType == SerializedPropertyType.Generic;

        private ReorderableList GetList(SerializedProperty property, GUIContent label)
        {
            string key = $"{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";
            if (_cache.TryGetValue(key, out var list))
                return list;

            var attr = (AesListLabelAttribute)attribute;

            list = new ReorderableList(property.serializedObject, property, true, true, true, true);

            list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, label);

            list.elementHeightCallback = index =>
            {
                var element = property.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 4f;
            };

            list.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = property.GetArrayElementAtIndex(index);
                string text = BuildLabel(element, attr.Members, index);
                EditorGUI.PropertyField(rect, element, new GUIContent(text), true);
            };

            _cache[key] = list;
            return list;
        }

        // ------------------------

        private string BuildLabel(SerializedProperty element, string[] members, int index)
        {
            if (members == null || members.Length == 0)
                return $"Element {index}";

            var parts = new List<string>();

            foreach (var m in members)
            {
                var p = element.FindPropertyRelative(m);
                if (p == null) continue;

                string v = PropertyToString(p);
                if (!string.IsNullOrEmpty(v))
                    parts.Add(v);
            }

            if (parts.Count == 0)
                return $"Element {index}";

            return string.Join(" / ", parts);
        }

        private string PropertyToString(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.String:
                    return p.stringValue;

                case SerializedPropertyType.Enum:
                    return p.enumDisplayNames[p.enumValueIndex];

                case SerializedPropertyType.Integer:
                    return p.intValue.ToString();

                case SerializedPropertyType.Boolean:
                    return p.boolValue ? "True" : "False";

                case SerializedPropertyType.ObjectReference:
                    return p.objectReferenceValue != null
                        ? p.objectReferenceValue.name
                        : "None";

                default:
                    return p.displayName;
            }
        }
    }
}
#endif
