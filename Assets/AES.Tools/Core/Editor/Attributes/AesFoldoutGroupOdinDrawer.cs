#if UNITY_EDITOR && ODIN_INSPECTOR
using System.Collections.Generic;
using AES.Tools;
using AES.Tools.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Gui.Editor
{
    public class AesFoldoutGroupOdinDrawer : OdinAttributeDrawer<AesFoldoutGroupAttribute>
    {
        // 인스턴스별 / 그룹별 폴드 상태
        private static readonly Dictionary<int, Dictionary<string, bool>> FoldoutStates 
            = new Dictionary<int, Dictionary<string, bool>>();

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var groupName = Attribute.GroupName ?? "<Group>";

            // 대상 인스턴스 ID
            Object unityTarget = Property.Tree.UnitySerializedObject != null
                ? Property.Tree.UnitySerializedObject.targetObject as Object
                : null;

            int id = unityTarget != null ? unityTarget.GetInstanceID() : Property.Tree.GetHashCode();

            if (!FoldoutStates.TryGetValue(id, out var groupDict))
            {
                groupDict = new Dictionary<string, bool>();
                FoldoutStates[id] = groupDict;
            }

            if (!groupDict.TryGetValue(groupName, out bool expanded))
                expanded = true;

            bool isHeader = AesOdinFoldoutUtil.IsFirstInGroup(Property, groupName);

            if (isHeader)
            {
                // 헤더 한 줄: 화살표 + 그룹 이름
                expanded = EditorGUILayout.Foldout(expanded, groupName, true);
                groupDict[groupName] = expanded;

                if (expanded)
                {
                    // 헤더 필드 (첫 번째 필드)
                    EditorGUI.indentLevel++;
                    CallNextDrawer(label);
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                // 같은 그룹의 나머지 필드들
                if (!expanded)
                    return;

                EditorGUI.indentLevel++;
                CallNextDrawer(label);
                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif
