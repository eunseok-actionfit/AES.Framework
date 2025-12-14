#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesInlineSOAttribute))]
    public sealed class AesInlineSODrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> Fold = new();
        private UnityEditor.Editor _cached;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var a = (AesInlineSOAttribute)attribute;

            // 1) Object field
            var line = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var objRect = new Rect(position.x, position.y, position.width, line);
            EditorGUI.PropertyField(objRect, property, label, true);

            // 2) Inline
            Object obj = property.objectReferenceValue;
            if (obj == null) return;
            if (obj is not ScriptableObject) return;

            float y = objRect.yMax + spacing;

            bool expanded = true;
            if (a.Foldout)
            {
                string key = $"{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";
                if (!Fold.TryGetValue(key, out expanded)) expanded = true;

                var foldRect = new Rect(position.x, y, position.width, line);
                expanded = EditorGUI.Foldout(foldRect, expanded, "Inline", true);
                Fold[key] = expanded;

                y = foldRect.yMax + spacing;
                if (!expanded) return;
            }

            // Cached editor
            UnityEditor.Editor.CreateCachedEditor(obj, null, ref _cached);
            if (_cached == null) return;

            // 3) Draw boxed inspector
            var boxRect = new Rect(position.x, y, position.width, GetInlineHeight(obj, a));
            GUI.Box(boxRect, GUIContent.none);

            var inner = new Rect(boxRect.x + 6, boxRect.y + 6, boxRect.width - 12, boxRect.height - 12);

            GUILayout.BeginArea(inner);
            if (a.DrawHeader)
                EditorGUILayout.LabelField(obj.name, EditorStyles.boldLabel);

            _cached.OnInspectorGUI();
            GUILayout.EndArea();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var a = (AesInlineSOAttribute)attribute;

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            float h = line; // object field

            Object obj = property.objectReferenceValue;
            if (obj == null || obj is not ScriptableObject)
                return h;

            if (a.Foldout)
            {
                h += spacing + line; // foldout line

                string key = $"{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";
                if (Fold.TryGetValue(key, out bool expanded) && !expanded)
                    return h;
            }

            h += spacing + GetInlineHeight(obj, a);
            return h;
        }

        private float GetInlineHeight(Object obj, AesInlineSOAttribute a)
        {
            // “안정판” 핵심: 정확한 높이 계산은 Unity가 불가능한 경우가 많음.
            // 따라서 최소 높이를 보장하고, 내용이 길면 스크롤/확장 없이도 크게 잡는다.
            // 필요하면 여기만 프로젝트 스타일로 튜닝하면 됨.
            float baseH = EditorGUIUtility.singleLineHeight * (a.DrawHeader ? 10f : 9f);
            return Mathf.Max(120f, baseH);
        }
    }
}
#endif
