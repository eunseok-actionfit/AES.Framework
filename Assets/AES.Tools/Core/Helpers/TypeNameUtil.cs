using System;
using System.Collections.Generic;

namespace AES.Tools
{
    internal static class TypeNameUtil
    {
        static readonly Dictionary<Type, string> s_builtinNames = new()
        {
            { typeof(float),   "float" },
            { typeof(double),  "double" },
            { typeof(int),     "int" },
            { typeof(uint),    "uint" },
            { typeof(long),    "long" },
            { typeof(ulong),   "ulong" },
            { typeof(short),   "short" },
            { typeof(ushort),  "ushort" },
            { typeof(byte),    "byte" },
            { typeof(sbyte),   "sbyte" },
            { typeof(bool),    "bool" },
            { typeof(char),    "char" },
            { typeof(string),  "string" },
            { typeof(decimal), "decimal" },
            { typeof(void),    "void" },
        };

        public static string GetFriendlyTypeName(Type t)
        {
            if (t == null)
                return "null";

            // 기본 타입: float / int / bool / string 등
            if (s_builtinNames.TryGetValue(t, out var alias))
                return alias;

            // 배열
            if (t.IsArray)
            {
                var elemType = t.GetElementType();
                return $"{GetFriendlyTypeName(elemType)}[]";
            }

            // 제네릭이 아닌 타입
            if (!t.IsGenericType)
                return t.Name;

            // 제네릭 타입
            var genericDef = t.IsGenericTypeDefinition ? t : t.GetGenericTypeDefinition();

            string genericName;

            // ObservableProperty<T>, IObservableProperty<T>를 보기 좋게 줄이고 싶다면
            if (genericDef == typeof(ObservableProperty<>)
                || genericDef.Name.StartsWith("ObservableProperty")
                || genericDef.Name.StartsWith("IObservableProperty"))
            {
                genericName = "Observable";
            }
            else
            {
                genericName = t.Name;
                int backtickIndex = genericName.IndexOf('`');
                if (backtickIndex >= 0)
                    genericName = genericName.Substring(0, backtickIndex);
            }

            var args     = t.GetGenericArguments();
            var argNames = new string[args.Length];

            for (int i = 0; i < args.Length; i++)
                argNames[i] = GetFriendlyTypeName(args[i]);

            return $"{genericName}<{string.Join(", ", argNames)}>";
        }
    }
}
