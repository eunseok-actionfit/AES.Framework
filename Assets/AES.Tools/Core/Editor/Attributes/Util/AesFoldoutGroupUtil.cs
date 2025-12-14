// #if UNITY_EDITOR
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
//
//
// namespace AES.Tools.Editor.Util
// {
//     public static class AesFoldoutGroupUtil
//     {
//         public static bool IsFirstFieldOfGroup(SerializedProperty property, string groupName)
//         {
//             if (property == null || property.serializedObject == null)
//                 return true;
//
//             var target = property.serializedObject.targetObject;
//             if (target == null)
//                 return true;
//
//             var type = target.GetType();
//             var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
//
//             // 필드 선언 순서 그대로
//             var fields = type.GetFields(flags);
//
//             FieldInfo firstGroupField = null;
//             FieldInfo currentField = null;
//
//             foreach (var f in fields)
//             {
//                 var attrs = f.GetCustomAttributes(typeof(AesFoldoutGroupAttribute), true)
//                              .Cast<AesFoldoutGroupAttribute>()
//                              .ToArray();
//
//                 if (f.Name == property.name)
//                     currentField = f;
//
//                 if (attrs.Length > 0)
//                 {
//                     foreach (var a in attrs)
//                     {
//                         if (a.GroupName == groupName)
//                         {
//                             if (firstGroupField == null)
//                                 firstGroupField = f;
//                             break;
//                         }
//                     }
//                 }
//             }
//
//             if (currentField == null || firstGroupField == null)
//                 return true;
//
//             return currentField == firstGroupField;
//         }
//
//         public static string GetGroupKey(SerializedProperty property, string groupName)
//         {
//             // 인스턴스마다 구분되게 targetInstanceID + groupName 조합
//             int id = property.serializedObject.targetObject != null
//                 ? property.serializedObject.targetObject.GetInstanceID()
//                 : 0;
//
//             return $"{id}:{groupName}";
//         }
//     }
// }
// #endif
