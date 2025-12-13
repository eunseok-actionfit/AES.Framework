#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;


namespace AES.Tools.Editor.Util
{
    public static class AesGUIColorHelper
    {
        public static bool TryGetColor(object target, AesGUIColorAttribute attr, out Color color)
        {
            color = Color.white;

            if (target == null || attr == null || string.IsNullOrEmpty(attr.ColorSource))
                return false;

            string src = attr.ColorSource.Trim();

            if (src.StartsWith("@"))
            {
                return TryEvalExpression(target, src.Substring(1).Trim(), out color);
            }

            // 멤버 기반
            return TryGetColorFromMember(target, src, out color);
        }

        private static bool TryGetColorFromMember(object target, string name, out Color color)
        {
            color = Color.white;
            var type  = target.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            MemberInfo member =
                type.GetField(name, flags) ??
                (MemberInfo)type.GetProperty(name, flags) ??
                type.GetMethod(name, flags, null, Type.EmptyTypes, null);

            if (member == null)
                return false;

            object value = null;
            if (member is FieldInfo f) value = f.GetValue(target);
            else if (member is PropertyInfo p) value = p.GetValue(target);
            else if (member is MethodInfo m) value = m.Invoke(target, null);

            if (value is Color c)
            {
                color = c;
                return true;
            }

            if (value is Color32 c32)
            {
                color = c32;
                return true;
            }

            return false;
        }

        private static bool TryEvalExpression(object target, string expr, out Color color)
        {
            color = Color.white;

            // 1) Color(...) / Color32(...) 리터럴
            if (expr.StartsWith("Color("))
            {
                var inside = GetParenContent(expr);
                if (inside == null) return false;

                var parts = inside.Split(',');
                if (parts.Length < 3 || parts.Length > 4) return false;

                float r = ParseFloat(parts[0]);
                float g = ParseFloat(parts[1]);
                float b = ParseFloat(parts[2]);
                float a = parts.Length >= 4 ? ParseFloat(parts[3]) : 1f;

                color = new Color(r, g, b, a);
                return true;
            }

            if (expr.StartsWith("Color32("))
            {
                var inside = GetParenContent(expr);
                if (inside == null) return false;

                var parts = inside.Split(',');
                if (parts.Length < 3 || parts.Length > 4) return false;

                byte r = (byte)ParseInt(parts[0]);
                byte g = (byte)ParseInt(parts[1]);
                byte b = (byte)ParseInt(parts[2]);
                byte a = parts.Length >= 4 ? (byte)ParseInt(parts[3]) : (byte)255;

                color = new Color32(r, g, b, a);
                return true;
            }

            // 2) Color.red, Color.green 같은 기본 색 이름 (간단 매핑)
            if (TryBuiltinColor(expr, out color))
                return true;

            // 3) 나머지는 멤버 이름으로 다시 시도: @MyColorField
            return TryGetColorFromMember(target, expr, out color);
        }

        private static string GetParenContent(string expr)
        {
            int open  = expr.IndexOf('(');
            int close = expr.LastIndexOf(')');
            if (open < 0 || close <= open) return null;
            return expr.Substring(open + 1, close - open - 1);
        }

        private static float ParseFloat(string s)
        {
            if (float.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var v))
                return v;

            return 0f;
        }

        private static int ParseInt(string s)
        {
            if (int.TryParse(s, out var v))
                return v;
            return 0;
        }

        private static bool TryBuiltinColor(string expr, out Color color)
        {
            color = Color.white;

            switch (expr)
            {
                case "Color.red": color = Color.red; return true;
                case "Color.green": color = Color.green; return true;
                case "Color.blue": color = Color.blue; return true;
                case "Color.white": color = Color.white; return true;
                case "Color.black": color = Color.black; return true;
                case "Color.yellow": color = Color.yellow; return true;
                case "Color.cyan": color = Color.cyan; return true;
                case "Color.magenta": color = Color.magenta; return true;
                case "Color.gray": color = Color.gray; return true;
                case "Color.grey": color = Color.grey; return true;
                case "Color.clear": color = Color.clear; return true;
                default: return false;
            }
        }
    }
}
#endif
