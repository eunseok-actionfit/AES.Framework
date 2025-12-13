using System;
using UnityEngine;


namespace AES.Tools
{
    [CreateAssetMenu(menuName = "AES/Converters/Float → Int")]
    public class FloatToIntConverter : ValueConverterSOBase
    {
        public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            if (value is float f)
                return Mathf.RoundToInt(f);

            if (value != null && float.TryParse(value.ToString(), out var parsed))
                return Mathf.RoundToInt(parsed);

            return 0;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            if (value is int i)
                return (float)i;

            if (value != null && int.TryParse(value.ToString(), out var parsed))
                return (float)parsed;

            return 0f;
        }
    }
}