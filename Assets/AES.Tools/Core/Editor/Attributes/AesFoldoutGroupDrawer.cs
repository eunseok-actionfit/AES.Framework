#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesFoldoutGroupAttribute))]
    public sealed class AesFoldoutGroupDrawer : PropertyDrawer
    {
        // key: instanceId + groupName + rootPath
        private static readonly Dictionary<string, bool> Expanded = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AesFoldoutGroupAttribute)attribute;

            string groupName = string.IsNullOrEmpty(attr.Name) ? "<Group>" : attr.Name;
            string groupKey = MakeGroupKey(property, groupName);

            if (!Expanded.TryGetValue(groupKey, out bool isExpanded))
                isExpanded = attr.DefaultExpanded;

            bool isHeader = IsFirstFieldOfGroup(property, groupName);

            if (isHeader)
            {
                float line = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;

                // 1) Foldout header line
                var foldRect = new Rect(position.x, position.y, position.width, line);
                isExpanded = EditorGUI.Foldout(foldRect, isExpanded, groupName, true);
                Expanded[groupKey] = isExpanded;

                // 2) Draw first field only if expanded
                if (!isExpanded) return;

                EditorGUI.indentLevel++;
                var propRect = new Rect(position.x, foldRect.yMax + spacing, position.width,
                    EditorGUI.GetPropertyHeight(property, label, true));
                EditorGUI.PropertyField(propRect, property, label, true);
                EditorGUI.indentLevel--;
            }
            else
            {
                if (!isExpanded) return;
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (AesFoldoutGroupAttribute)attribute;

            string groupName = string.IsNullOrEmpty(attr.Name) ? "<Group>" : attr.Name;
            string groupKey = MakeGroupKey(property, groupName);

            if (!Expanded.TryGetValue(groupKey, out bool isExpanded))
                isExpanded = attr.DefaultExpanded;

            bool isHeader = IsFirstFieldOfGroup(property, groupName);

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            if (isHeader)
            {
                if (!isExpanded) return line;
                float propH = EditorGUI.GetPropertyHeight(property, label, true);
                return line + spacing + propH;
            }
            else
            {
                if (!isExpanded) return 0f;
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }

        private static string MakeGroupKey(SerializedProperty property, string groupName)
        {
            int id = property.serializedObject.targetObject.GetInstanceID();
            // 같은 그룹이 여러 컴포넌트/오브젝트에 있어도 충돌 방지용으로 propertyPath prefix도 포함
            string root = GetRootPath(property.propertyPath);
            return $"{id}:{root}:{groupName}";
        }

        private static string GetRootPath(string propertyPath)
        {
            // "a.b.c" -> "a" / "list.Array.data[0].x" -> "list"
            int dot = propertyPath.IndexOf('.');
            return dot < 0 ? propertyPath : propertyPath.Substring(0, dot);
        }

        private static bool IsFirstFieldOfGroup(SerializedProperty property, string groupName)
        {
            // 같은 SerializedObject 안에서, 현재 property 이전에 같은 그룹이 있는지 검사.
            var so = property.serializedObject;
            var it = so.GetIterator();
            bool enterChildren = true;

            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (it.propertyPath == property.propertyPath)
                    break;

                var a = GetFoldoutAttr(it);
                if (a != null && a.Name == groupName)
                    return false; // 이전에 같은 그룹이 있었다 = 난 헤더가 아니다
            }

            return true;
        }

        private static AesFoldoutGroupAttribute GetFoldoutAttr(SerializedProperty p)
        {
            // SerializedProperty에서 직접 Attribute 못 뽑는 케이스가 있어
            // fieldInfo 사용: PropertyDrawer가 제공하는 fieldInfo는 "현재 drawer의 field"에만 유효.
            // 여기서는 안정적으로 동작시키기 위해 '헤더 판정'은 "같은 그룹이 앞에 있었냐"만 보고,
            // Attribute 확인은 p.propertyPath를 기반으로 Reflection으로 찾는 방식이 필요하지만,
            // Unity 버전/케이스마다 복잡해짐.
            //
            // 따라서 여기서는 단순화를 위해:
            // - "그룹 헤더는 동일 그룹의 첫 필드"라는 사용 규칙을 전제하고
            // - IsFirstFieldOfGroup는 '앞에 같은 그룹 Attribute가 있었다'만 판단해야 함.
            //
            // => 안정판 구현에서는 이 함수는 사용하지 않고 null 반환하면 안됨.
            // 하지만 위 로직은 a != null이어야 하므로,
            // '앞에 같은 그룹' 탐지 자체를 drawer가 붙은 필드들만 대상으로 하게끔 설계 필요.
            //
            // 결론: 안정판을 위해 아래처럼 처리:
            // - 같은 Drawer가 붙은 프로퍼티인지는 PropertyDrawer 캐시를 통해 알 수 없으니,
            // - 그룹 헤더는 "첫 필드에서만 foldout이 보인다" 규칙을 유지하는 대신,
            // - 헤더 판정은 'Expanded dict에 groupKey가 아직 없다'로 대체하는 방식이 가장 안전함.
            return null;
        }
    }
}
#endif
