#if UNITY_EDITOR && ODIN_INSPECTOR
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace AES.Tools.Editor
{
    public static class AesOdinFoldoutUtil
    {
        // object + groupName 조합마다 "첫 번째 프로퍼티 Path" 저장
        private static readonly Dictionary<string, string> FirstPropertyPath = new();

        public static bool IsFirstInGroup(InspectorProperty property, string groupName)
        {
            if (property == null || property.Tree == null)
                return true;

            // 인스턴스 ID 구하기 (없으면 Tree 해시 사용)
            Object unityTarget = property.Tree.UnitySerializedObject != null
                ? property.Tree.UnitySerializedObject.targetObject as Object
                : null;

            int id = unityTarget != null ? unityTarget.GetInstanceID() : property.Tree.GetHashCode();

            string key = $"{id}:{groupName}";

            // 아직 이 그룹에 대한 "첫 프로퍼티"가 기록 안돼 있으면 지금 프로퍼티가 첫 번째
            if (!FirstPropertyPath.TryGetValue(key, out var path))
            {
                FirstPropertyPath[key] = property.Path;
                path = property.Path;
            }

            // 현재 그리는 프로퍼티가 기록된 첫 번째 Path 인가?
            return property.Path == path;
        }
    }
}
#endif