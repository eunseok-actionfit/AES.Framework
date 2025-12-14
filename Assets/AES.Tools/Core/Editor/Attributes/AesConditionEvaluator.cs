// #if UNITY_EDITOR
// using System;
// using System.Reflection;
// using UnityEngine;
//
// namespace AES.Tools.Editor
// {
//     public static class AesConditionEvaluator
//     {
//         /// <summary>
//         /// 조건을 bool로 평가.
//         /// invert 여부는 여기서 처리하지 않고, 각 Drawer에서 처리.
//         /// </summary>
//         public static bool EvaluateRaw(object target, AesConditionAttributeBase attr)
//         {
//             if (target == null || attr == null)
//                 return true;
//
//             if (attr.IsExpression)
//                 return EvaluateExpression(target, attr.Condition.Substring(1));
//
//             return EvaluateMemberCondition(target, attr);
//         }
//
//         private static bool EvaluateMemberCondition(object target, AesConditionAttributeBase attr)
//         {
//             var type = target.GetType();
//             var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
//             string name = attr.Condition;
//
//             var field = type.GetField(name, flags);
//             if (field != null)
//                 return CompareValue(field.GetValue(target), attr);
//
//             var prop = type.GetProperty(name, flags);
//             if (prop != null)
//                 return CompareValue(prop.GetValue(target), attr);
//
//             var method = type.GetMethod(name, flags, null, Type.EmptyTypes, null);
//             if (method != null)
//             {
//                 var value = method.Invoke(target, null);
//                 return CompareValue(value, attr);
//             }
//
//             // 못 찾으면 그냥 true
//             return true;
//         }
//
//         private static bool CompareValue(object value, AesConditionAttributeBase attr)
//         {
//             // 비교값 없음: bool이면 그대로, 그 외 참조타입이면 null 여부로
//             if (!attr.HasCompareValue)
//             {
//                 if (value is bool b)
//                     return b;
//
//                 if (!(value is ValueType))
//                     return value != null;
//
//                 return true;
//             }
//
//             // 비교값 있음
//             if (value == null && attr.CompareValue == null)
//                 return true;
//             if (value == null || attr.CompareValue == null)
//                 return false;
//
//             if (value.GetType().IsEnum && attr.CompareValue.GetType().IsPrimitive)
//             {
//                 try
//                 {
//                     var converted = Enum.ToObject(value.GetType(), attr.CompareValue);
//                     return Equals(value, converted);
//                 }
//                 catch { }
//             }
//
//             return Equals(value, attr.CompareValue);
//         }
//
//         // 아래부터는 간단 표현식용 (&&, ||, !, ==, !=, null, EnumName) 등
//         private static bool EvaluateExpression(object target, string expr)
//         {
//             expr = expr.Replace("this.", string.Empty);
//             try
//             {
//                 return SimpleBoolExpression(target, expr);
//             }
//             catch (Exception e)
//             {
//                 Debug.LogWarning($"AesCondition expression error: {e.Message}\nExpr: {expr}");
//                 return true;
//             }
//         }
//
//         private static bool SimpleBoolExpression(object target, string expr)
//         {
//             var orParts = expr.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
//
//             foreach (var orPart in orParts)
//             {
//                 var andParts = orPart.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
//                 bool andResult = true;
//
//                 foreach (var andPartRaw in andParts)
//                 {
//                     string andPart = andPartRaw.Trim();
//                     if (string.IsNullOrEmpty(andPart))
//                         continue;
//
//                     bool negate = false;
//                     while (andPart.StartsWith("!"))
//                     {
//                         negate = !negate;
//                         andPart = andPart.Substring(1).Trim();
//                     }
//
//                     bool cond = EvaluateSimpleToken(target, andPart);
//                     if (negate) cond = !cond;
//
//                     andResult &= cond;
//                     if (!andResult)
//                         break;
//                 }
//
//                 if (andResult)
//                     return true;
//             }
//
//             return false;
//         }
//
//         private static bool EvaluateSimpleToken(object target, string token)
//         {
//             token = token.Trim();
//
//             if (token.Contains("=="))
//             {
//                 var split = token.Split(new[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
//                 if (split.Length == 2)
//                     return CompareMemberToLiteral(target, split[0].Trim(), split[1].Trim(), equal: true);
//             }
//             else if (token.Contains("!="))
//             {
//                 var split = token.Split(new[] { "!=" }, StringSplitOptions.RemoveEmptyEntries);
//                 if (split.Length == 2)
//                     return CompareMemberToLiteral(target, split[0].Trim(), split[1].Trim(), equal: false);
//             }
//
//             if (token.EndsWith("!= null") || token.EndsWith("!=null"))
//             {
//                 var name = token.Replace("!= null", string.Empty).Replace("!=null", string.Empty).Trim();
//                 return CompareMemberToLiteral(target, name, "null", equal: false);
//             }
//             if (token.EndsWith("== null") || token.EndsWith("==null"))
//             {
//                 var name = token.Replace("== null", string.Empty).Replace("==null", string.Empty).Trim();
//                 return CompareMemberToLiteral(target, name, "null", equal: true);
//             }
//
//             // 단순 멤버
//             return EvaluateMemberCondition(target, new TempShowIfAttribute(token));
//         }
//
//         // 내부용 임시 Attribute
//         private class TempShowIfAttribute : AesConditionAttributeBase
//         {
//             public TempShowIfAttribute(string condition) : base(condition) { }
//         }
//
//         private static bool CompareMemberToLiteral(object target, string memberName, string literal, bool equal)
//         {
//             var type = target.GetType();
//             var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
//
//             MemberInfo member =
//                 type.GetField(memberName, flags) ??
//                 (MemberInfo)type.GetProperty(memberName, flags) ??
//                 type.GetMethod(memberName, flags, null, Type.EmptyTypes, null);
//
//             if (member == null)
//                 return true;
//
//             object value = null;
//             if (member is FieldInfo f) value = f.GetValue(target);
//             else if (member is PropertyInfo p) value = p.GetValue(target);
//             else if (member is MethodInfo m) value = m.Invoke(target, null);
//
//             if (literal == "null")
//             {
//                 bool isNull = value == null;
//                 return equal ? isNull : !isNull;
//             }
//
//             if (value != null)
//             {
//                 var vType = value.GetType();
//
//                 if (vType.IsEnum)
//                 {
//                     try
//                     {
//                         string enumName = literal;
//                         int dot = literal.IndexOf('.');
//                         if (dot >= 0 && dot < literal.Length - 1)
//                             enumName = literal.Substring(dot + 1);
//
//                         var parsed = Enum.Parse(vType, enumName);
//                         bool eq = Equals(value, parsed);
//                         return equal ? eq : !eq;
//                     }
//                     catch { }
//                 }
//
//                 if (vType == typeof(string))
//                 {
//                     bool eq = string.Equals((string)value, literal.Trim('"'), StringComparison.Ordinal);
//                     return equal ? eq : !eq;
//                 }
//
//                 if (double.TryParse(literal, out var num))
//                 {
//                     try
//                     {
//                         double val = Convert.ToDouble(value);
//                         bool eq = Math.Abs(val - num) < double.Epsilon;
//                         return equal ? eq : !eq;
//                     }
//                     catch { }
//                 }
//             }
//
//             bool eqStr = string.Equals(value?.ToString(), literal, StringComparison.Ordinal);
//             return equal ? eqStr : !eqStr;
//         }
//     }
// }
// #endif
