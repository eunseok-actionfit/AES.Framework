using System;
using System.Globalization;
using UnityEngine;

namespace AES.Tools
{
    [CreateAssetMenu(
        menuName = "AES/ValueConverter/Language Converter",
        fileName = "LanguageConverter")]
    public sealed class LanguageConverterSO : ValueConverterSOBase
    {
        public override object Convert(
            object value,
            Type targetType,
            object parameter,
            IFormatProvider culture)
        {
            if (value is not string code)
                return string.Empty;

            return code switch
            {
                "en" or "en-US" => "English",
                "ko" or "ko-KR" => "한국어",
                "ja" or "ja-JP" => "日本語",
                _ => code
            };
        }

        public override object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            IFormatProvider culture)
        {
            if (value is not string text)
                throw new NotSupportedException();

            return text switch
            {
                "English" => "en",
                "한국어" => "ko-KR",
                "日本語" => "ja-JP",
                _ => text
            };
        }
    }
}