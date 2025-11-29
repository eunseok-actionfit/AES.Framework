// SharedUtility.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AES.Tools
{
    internal static class SharedUtility
    {
        private sealed class SharedField
        {
            public Component Component;
            public FieldInfo FieldInfo;
            public SerializedObject SerializedObject;
            public SerializedProperty RootProp;
            public SerializedProperty ValueProp;
            public SerializedProperty IsSourceProp;
            public SerializedProperty ScopeProp;
            public SerializedProperty GroupProp;
            public string GroupKey;
            public SharedScope Scope;
            public Type ValueType;
        }

        /// <summary>
        /// 해당 Shared<T>를 소스로 보고 그룹 전체 동기화.
        /// </summary>
        public static void SyncFromThisAsSource(SerializedProperty sharedProp)
        {
            SyncInternal(sharedProp, true);
        }

        /// <summary>
        /// 값 변경 시 자동 동기화 (소스 자동 선택 규칙 사용).
        /// </summary>
        public static void AutoSyncGroup(SerializedProperty sharedProp)
        {
            SyncInternal(sharedProp, false);
        }

        private static void SyncInternal(SerializedProperty sharedProp, bool forceThisAsSource)
        {
            if (sharedProp == null)
                return;

            var so = sharedProp.serializedObject;
            var comp = so.targetObject as Component;
            if (!comp) return;

            var valueProp = sharedProp.FindPropertyRelative("value");
            var isSourceProp = sharedProp.FindPropertyRelative("isSource");
            var scopeProp = sharedProp.FindPropertyRelative("scope");
            var groupProp = sharedProp.FindPropertyRelative("groupId");

            if (valueProp == null || isSourceProp == null || scopeProp == null || groupProp == null)
                return;

            var scope = (SharedScope)scopeProp.enumValueIndex;
            var groupKey = BuildGroupKey(sharedProp, groupProp);

            var valueType = GetSharedValueType(sharedProp);
            if (valueType == null)
                return;

            var all = FindAllSharedFields(comp.gameObject, valueType, scope);
            var sameGroup = all.Where(f => f.GroupKey == groupKey).ToList();
            if (sameGroup.Count == 0)
                return;

            SharedField sourceField = null;

            if (forceThisAsSource)
            {
                sourceField = sameGroup.FirstOrDefault(f =>
                    f.SerializedObject.targetObject == sharedProp.serializedObject.targetObject &&
                    f.RootProp.propertyPath == sharedProp.propertyPath);

                if (sourceField != null)
                {
                    sourceField.IsSourceProp.boolValue = true;
                    sourceField.SerializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                // (1) 명시적으로 isSource && 값 있음
                sourceField = sameGroup.FirstOrDefault(f =>
                    f.IsSourceProp.boolValue && !IsNullOrDefault(f.ValueProp));
                // (2) 없으면 값 있는 첫 필드
                if (sourceField == null)
                    sourceField = sameGroup.FirstOrDefault(f => !IsNullOrDefault(f.ValueProp));
            }

            if (sourceField == null)
                return;

            var sourceValue = GetValueFromProperty(sourceField.ValueProp, valueType);

            foreach (var f in sameGroup)
            {
                if (ReferenceEquals(f, sourceField))
                    continue;

                if (!IsNullOrDefault(f.ValueProp) && !forceThisAsSource)
                    continue; // 이미 값 있으면 유지 (원하면 항상 덮어쓰도록 변경 가능)

                f.SerializedObject.Update();
                SetValueToProperty(f.ValueProp, sourceValue);
                f.SerializedObject.ApplyModifiedProperties();
            }
        }

        // ───────── helpers ─────────

        private static IEnumerable<GameObject> EnumerateScope(GameObject origin, SharedScope scope)
        {
            switch (scope)
            {
                case SharedScope.GameObject:
                    yield return origin;
                    break;

                case SharedScope.ParentAndChildren:
                    foreach (var t in origin.GetComponentsInParent<Transform>(true))
                        yield return t.gameObject;
                    foreach (var t in origin.GetComponentsInChildren<Transform>(true))
                        yield return t.gameObject;
                    break;

                case SharedScope.Scene:
                    var scene = origin.scene;
                    if (!scene.IsValid()) yield break;
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        yield return t.gameObject;
                    break;
            }
        }

        private static List<SharedField> FindAllSharedFields(GameObject origin, Type valueType, SharedScope scope)
        {
            var result = new List<SharedField>();

            foreach (var go in EnumerateScope(origin, scope))
            {
                var comps = go.GetComponents<MonoBehaviour>();
                foreach (var comp in comps)
                {
                    if (!comp) continue;

                    var type = comp.GetType();
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var f in fields)
                    {
                        if (!f.FieldType.IsGenericType)
                            continue;
                        if (f.FieldType.GetGenericTypeDefinition() != typeof(Shared<>))
                            continue;

                        var genericArg = f.FieldType.GetGenericArguments()[0];
                        if (genericArg != valueType)
                            continue;

                        var so = new SerializedObject(comp);
                        var root = so.FindProperty(f.Name);
                        if (root == null) { so.Dispose(); continue; }

                        var valueProp = root.FindPropertyRelative("value");
                        var isSourceProp = root.FindPropertyRelative("isSource");
                        var scopeProp = root.FindPropertyRelative("scope");
                        var groupProp = root.FindPropertyRelative("groupId");
                        if (valueProp == null || isSourceProp == null || scopeProp == null || groupProp == null)
                        {
                            so.Dispose();
                            continue;
                        }

                        var entry = new SharedField
                        {
                            Component = comp,
                            FieldInfo = f,
                            SerializedObject = so,
                            RootProp = root,
                            ValueProp = valueProp,
                            IsSourceProp = isSourceProp,
                            ScopeProp = scopeProp,
                            GroupProp = groupProp,
                            Scope = (SharedScope)scopeProp.enumValueIndex,
                            ValueType = genericArg,
                            GroupKey = BuildGroupKey(root, groupProp)
                        };
                        result.Add(entry);
                    }
                }
            }

            return result;
        }

        private static string BuildGroupKey(SerializedProperty sharedProp, SerializedProperty groupProp)
        {
            var gid = groupProp.stringValue;
            if (!string.IsNullOrEmpty(gid))
                return gid;

            // groupId 비면 필드 이름 사용 (propertyPath 마지막 토큰)
            var path = sharedProp.propertyPath;
            var idx = path.LastIndexOf('.');
            return idx >= 0 ? path[(idx + 1)..] : path;
        }

        private static Type GetSharedValueType(SerializedProperty sharedProp)
        {
            var ownerType = sharedProp.serializedObject.targetObject.GetType();
            var fi = ownerType.GetField(sharedProp.name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null) return null;
            if (!fi.FieldType.IsGenericType) return null;
            if (fi.FieldType.GetGenericTypeDefinition() != typeof(Shared<>)) return null;
            return fi.FieldType.GetGenericArguments()[0];
        }

        private static bool IsNullOrDefault(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return p.objectReferenceValue == null;
                case SerializedPropertyType.Boolean:
                    return !p.boolValue;
                case SerializedPropertyType.Integer:
                    return p.intValue == 0;
                case SerializedPropertyType.Float:
                    return Mathf.Approximately(p.floatValue, 0);
                case SerializedPropertyType.String:
                    return string.IsNullOrEmpty(p.stringValue);
                default:
                    return false;
            }
        }

        private static object GetValueFromProperty(SerializedProperty p, Type valueType)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return p.objectReferenceValue;
                case SerializedPropertyType.Boolean:
                    return p.boolValue;
                case SerializedPropertyType.Integer:
                    return p.intValue;
                case SerializedPropertyType.Float:
                    return p.floatValue;
                case SerializedPropertyType.String:
                    return p.stringValue;
            }
            return null;
        }

        private static void SetValueToProperty(SerializedProperty p, object value)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    p.objectReferenceValue = value as UnityEngine.Object;
                    break;
                case SerializedPropertyType.Boolean:
                    if (value is bool b) p.boolValue = b;
                    break;
                case SerializedPropertyType.Integer:
                    if (value is int i) p.intValue = i;
                    break;
                case SerializedPropertyType.Float:
                    if (value is float f) p.floatValue = f;
                    break;
                case SerializedPropertyType.String:
                    p.stringValue = value as string ?? "";
                    break;
            }
        }
    }
}
#endif
