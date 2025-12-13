#if UNITY_EDITOR && !ODIN_INSPECTOR
using System.Collections.Generic;
using AES.Tools.Editor.Util;
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesFoldoutGroupAttribute))]
    public class AesFoldoutGroupDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> FoldoutStates = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr      = (AesFoldoutGroupAttribute)attribute;
            var groupName = string.IsNullOrEmpty(attr.GroupName) ? "<Group>" : attr.GroupName;
            var groupKey  = AesFoldoutGroupUtil.GetGroupKey(property, groupName);

            if (!FoldoutStates.TryGetValue(groupKey, out var expanded))
                expanded = true;

            bool isHeader = AesFoldoutGroupUtil.IsFirstFieldOfGroup(property, groupName);

            if (isHeader)
            {
                EditorGUI.BeginProperty(position, label, property);

                float lineHeight = EditorGUIUtility.singleLineHeight;
                float spacing    = EditorGUIUtility.standardVerticalSpacing;

                // 폴드아웃 한 줄
                var foldRect = new Rect(
                    position.x,
                    position.y,
                    position.width,
                    lineHeight
                );

                expanded = EditorGUI.Foldout(foldRect, expanded, groupName, true);
                FoldoutStates[groupKey] = expanded;

                // 펼쳐진 경우 첫 번째 필드 그리기
                if (expanded)
                {
                    EditorGUI.indentLevel++;

                    var propRect = new Rect(
                        position.x,
                        foldRect.yMax + spacing,
                        position.width,
                        EditorGUI.GetPropertyHeight(property, label, true)
                    );

                    EditorGUI.PropertyField(propRect, property, label, true);

                    EditorGUI.indentLevel--;
                }

                EditorGUI.EndProperty();
            }
            else
            {
                // 같은 그룹 내 나머지 필드들
                if (!expanded)
                    return;

                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr      = (AesFoldoutGroupAttribute)attribute;
            var groupName = string.IsNullOrEmpty(attr.GroupName) ? "<Group>" : attr.GroupName;
            var groupKey  = AesFoldoutGroupUtil.GetGroupKey(property, groupName);

            if (!FoldoutStates.TryGetValue(groupKey, out var expanded))
                expanded = true;

            bool isHeader = AesFoldoutGroupUtil.IsFirstFieldOfGroup(property, groupName);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing    = EditorGUIUtility.standardVerticalSpacing;

            if (isHeader)
            {
                // 헤더(폴드아웃 줄)만 보이는 경우
                if (!expanded)
                    return lineHeight;

                // 펼쳐진 경우: 폴드아웃 한 줄 + 첫 필드 높이
                float propH = EditorGUI.GetPropertyHeight(property, label, true);
                return lineHeight + spacing + propH;
            }
            else
            {
                // 접힌 상태에서는 나머지 필드는 숨김
                if (!expanded)
                    return 0f;

                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }
    }
}
#endif
