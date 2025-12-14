#if UNITY_EDITOR
using System.Reflection;


namespace AES.Tools.Editor
{
    internal static class AesConditionUtil
    {
        public static bool Evaluate(object target, string member)
        {
            if (target == null || string.IsNullOrEmpty(member))
                return true;

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var type = target.GetType();

            var field = type.GetField(member, flags);
            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(target);

            var prop = type.GetProperty(member, flags);
            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(target);

            var method = type.GetMethod(member, flags, null, System.Type.EmptyTypes, null);
            if (method != null && method.ReturnType == typeof(bool))
                return (bool)method.Invoke(target, null);

            return true;
        }
    }
}
#endif