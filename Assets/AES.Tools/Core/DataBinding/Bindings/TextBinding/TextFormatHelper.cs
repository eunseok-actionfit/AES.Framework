using System;
using System.Globalization;

namespace AES.Tools
{
    /// <summary>
    /// TextBinding 계열이 공통으로 사용하는 포맷 헬퍼.
    /// 숫자/날짜 등 IFormattable 타입은 format + IFormatProvider 기반,
    /// 그 외 타입은 ToString() 사용.
    /// </summary>
    public static class TextFormatHelper
    {
        /// <summary>
        /// 값 -> 문자열 변환 (포맷 사용 여부 포함, 제네릭 버전)
        /// </summary>
        public static string Format<T>(
            T value,
            bool useFormat,
            string format,
            IFormatProvider provider = null)
        {
            provider ??= CultureInfo.InvariantCulture;

            if (useFormat && value is IFormattable formattable)
                return formattable.ToString(format, provider);

            return ConvertToString(value, provider);
        }

        /// <summary>
        /// 값 -> 문자열 변환 (object 버전, 바인딩에서 박싱된 값을 넘길 때 사용)
        /// </summary>
        public static string Format(
            object value,
            bool useFormat,
            string format,
            IFormatProvider provider = null)
        {
            provider ??= CultureInfo.InvariantCulture;

            if (value is IFormattable formattable && useFormat)
                return formattable.ToString(format, provider);

            return ConvertToString(value, provider);
        }

        /// <summary>
        /// 포맷 없이 순수 문자열 변환 (object)
        /// </summary>
        public static string ConvertToString(
            object value,
            IFormatProvider provider = null)
        {
            provider ??= CultureInfo.InvariantCulture;

            if (value == null)
                return string.Empty;

            if (value is IFormattable formattable)
                return formattable.ToString(null, provider);

            return value.ToString() ?? string.Empty;
        }

        /// <summary>
        /// 포맷 없이 순수 문자열 변환 (제네릭)
        /// </summary>
        public static string ConvertToString<T>(
            T value,
            IFormatProvider provider = null)
        {
            provider ??= CultureInfo.InvariantCulture;

            if (value == null)
                return string.Empty;

            if (value is IFormattable formattable)
                return formattable.ToString(null, provider);

            return value.ToString() ?? string.Empty;
        }
    }
}
