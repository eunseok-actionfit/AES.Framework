using System;
using System.Globalization;


namespace AES.Tools
{
    public static class TextFormatHelper
    {
        /// <summary>
        /// 값 하나를 문자열로 변환.
        /// - useFormat == false : value.ToString() (IFormattable 고려)
        /// - useFormat == true  : string.Format(format, value) 그대로 사용
        ///   (format이 null/빈 문자열이면 "{0}"로 처리)
        /// </summary>
        public static string Format(
            object value,
            bool useFormat,
            string format,
            IFormatProvider provider = null)
        {
            provider ??= CultureInfo.InvariantCulture;

            if (!useFormat)
                return ConvertToString(value, provider);

            if (value == null)
                return string.Empty;

            var fmt = string.IsNullOrEmpty(format) ? "{0}" : format;
            
            return string.Format(provider, fmt, value);
        }

        public static string ConvertToString(object value, IFormatProvider provider)
        {
            provider ??= CultureInfo.InvariantCulture;

            if (value == null)
                return string.Empty;

            if (value is IFormattable f)
                return f.ToString(null, provider);

            return value.ToString() ?? string.Empty;
        }
    }
}