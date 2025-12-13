using System;
using System.Reflection;
using AYellowpaper.SerializedCollections;


namespace AES.Tools
{
    public static class DictFromAttributes
    {
        public static SerializedDictionary<string, TValue> CreateFromStringKeys<TValue>(Type keysType)
        {
            var dict = new SerializedDictionary<string, TValue>();

            var fields = keysType.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
            {
                if (!field.IsLiteral || field.FieldType != typeof(string))
                    continue;

                var attr = field.GetCustomAttribute<DictKeyAttribute>();
                if (attr == null)
                    continue;

                var key = (string)field.GetRawConstantValue();

                TValue defaultValue = default;

                if (attr.DefaultValue is TValue typed)
                {
                    defaultValue = typed;
                }
                else if (attr.DefaultValue != null)
                {
                    try
                    {
                        defaultValue = (TValue)Convert.ChangeType(attr.DefaultValue, typeof(TValue));
                    }
                    catch
                    {
                        // 변환 실패 시 default(TValue) 유지
                    }
                }

                dict[key] = defaultValue;
            }

            return dict;
        }
    

        public static SerializedDictionary<TEnum, TValue> CreateFromEnum<TEnum, TValue>()
            where TEnum : Enum
        {
            var dict = new SerializedDictionary<TEnum, TValue>();

            var fields = typeof(TEnum)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
            {
                if (!field.IsLiteral) // enum 멤버
                    continue;

                var attr = field.GetCustomAttribute<DictKeyAttribute>();
                if (attr == null)
                    continue;

                var enumValue = (TEnum)field.GetValue(null);

                TValue defaultValue = default;

                if (attr.DefaultValue is TValue typed)
                {
                    defaultValue = typed;
                }
                else if (attr.DefaultValue != null)
                {
                    try
                    {
                        defaultValue = (TValue)Convert.ChangeType(attr.DefaultValue, typeof(TValue));
                    }
                    catch
                    { // ignored
                    }
                }

                dict[enumValue] = defaultValue;
            }

            return dict;
        }

    }
}