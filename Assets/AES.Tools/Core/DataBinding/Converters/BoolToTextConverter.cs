using System;
using System.Globalization;
using UnityEngine;


namespace AES.Tools
{
    [CreateAssetMenu(menuName = "AES/ValueConverters/Bool To Text")]
    public class BoolToTextConverter : ValueConverterSOBase
    {
        [SerializeField] string trueText = "On";
        [SerializeField] string falseText = "Off";

        public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            if (value is bool b)
                return b ? trueText : falseText;

            return value?.ToString() ?? string.Empty;
        }
    }
}