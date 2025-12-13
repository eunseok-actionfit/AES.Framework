using System;
using System.Globalization;
using UnityEngine;


namespace AES.Tools
{
    [CreateAssetMenu(menuName = "AES/Converters/Auto Converter")]
    public class AutoConverter : ValueConverterSOBase
    {
        public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            return ConvertCore(value, targetType, provider);
        }

        public override object ConvertBack(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            // 여기서는 Convert와 동일한 로직을 그대로 재사용
            return ConvertCore(value, targetType, provider);
        }

        object ConvertCore(object value, Type targetType, IFormatProvider provider)
        {
            if (targetType == null)
                return value;

            if (value == null)
            {
                // 값형이면 default, 참조형이면 null
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }

            var srcType = value.GetType();
            if (targetType.IsAssignableFrom(srcType))
                return value; // 이미 타입이 맞음

            // enum → underlying numeric
            if (srcType.IsEnum)
            {
                value = System.Convert.ToInt32(value, provider ?? CultureInfo.InvariantCulture);
                srcType = typeof(int);
            }

            // string으로 들어온 경우 숫자/bool 파싱
            if (value is string s)
            {
                if (targetType == typeof(int) && int.TryParse(s, NumberStyles.Any, provider ?? CultureInfo.InvariantCulture, out var i))
                    return i;

                if (targetType == typeof(float) && float.TryParse(s, NumberStyles.Any, provider ?? CultureInfo.InvariantCulture, out var f))
                    return f;

                if (targetType == typeof(double) && double.TryParse(s, NumberStyles.Any, provider ?? CultureInfo.InvariantCulture, out var d))
                    return d;

                if (targetType == typeof(bool) && bool.TryParse(s, out var b))
                    return b;
            }

            // bool 변환
            if (targetType == typeof(bool))
            {
                if (value is bool bb) return bb;

                if (IsNumericType(srcType))
                {
                    double num = System.Convert.ToDouble(value, provider ?? CultureInfo.InvariantCulture);
                    return System.Math.Abs(num) > double.Epsilon;
                }

                // 나머지는 "true"/"false" 정도만 시도
                if (value is string bs && bool.TryParse(bs, out var parsedBool))
                    return parsedBool;

                return false;
            }

            // float 변환
            if (targetType == typeof(float))
            {
                try
                {
                    double d = System.Convert.ToDouble(value, provider ?? CultureInfo.InvariantCulture);
                    return (float)d;
                }
                catch
                {
                    return 0f;
                }
            }

            // double 변환
            if (targetType == typeof(double))
            {
                try
                {
                    double d = System.Convert.ToDouble(value, provider ?? CultureInfo.InvariantCulture);
                    return d;
                }
                catch
                {
                    return 0d;
                }
            }

            // int 변환
            if (targetType == typeof(int))
            {
                try
                {
                    double d = System.Convert.ToDouble(value, provider ?? CultureInfo.InvariantCulture);
                    return System.Convert.ToInt32(System.Math.Round(d));
                }
                catch
                {
                    return 0;
                }
            }

            // enum으로 변환
            if (targetType.IsEnum)
            {
                try
                {
                    if (IsNumericType(srcType))
                    {
                        int iv = System.Convert.ToInt32(value, provider ?? CultureInfo.InvariantCulture);
                        return Enum.ToObject(targetType, iv);
                    }

                    if (value is string es)
                        return Enum.Parse(targetType, es, ignoreCase: true);
                }
                catch
                {
                    // 실패 시 첫 번째 값
                    var names = Enum.GetValues(targetType);
                    return names.Length > 0 ? names.GetValue(0) : Activator.CreateInstance(targetType);
                }
            }

            // string 변환
            if (targetType == typeof(string))
                return value.ToString();

            // 마지막 시도: System.Convert.ChangeType
            try
            {
                return System.Convert.ChangeType(value, targetType, provider ?? CultureInfo.InvariantCulture);
            }
            catch
            {
                // 실패하면 그냥 targetType의 기본값
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        bool IsNumericType(Type t)
        {
            return t == typeof(byte) || t == typeof(sbyte) ||
                   t == typeof(short) || t == typeof(ushort) ||
                   t == typeof(int) || t == typeof(uint) ||
                   t == typeof(long) || t == typeof(ulong) ||
                   t == typeof(float) || t == typeof(double) ||
                   t == typeof(decimal);
        }
    }
}
