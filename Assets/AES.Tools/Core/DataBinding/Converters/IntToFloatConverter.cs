using System;
using AES.Tools;
using UnityEngine;

[CreateAssetMenu(menuName = "AES/Converters/Int → Float")]
public class IntToFloatConverter : ValueConverterSOBase
{
    public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
    {
        if (value is int i)
            return (float)i;

        if (value != null && int.TryParse(value.ToString(), out var parsed))
            return (float)parsed;

        return 0f;
    }

    public override object ConvertBack(object value, Type targetType, object parameter, IFormatProvider provider)
    {
        if (value is float f)
            return Mathf.RoundToInt(f);

        if (value != null && float.TryParse(value.ToString(), out var parsed))
            return Mathf.RoundToInt(parsed);

        return 0;
    }
}