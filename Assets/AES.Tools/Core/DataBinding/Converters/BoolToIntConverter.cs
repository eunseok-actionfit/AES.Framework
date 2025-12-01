using System;
using AES.Tools;
using UnityEngine;

[CreateAssetMenu(menuName = "AES/Converters/Bool ↔ Int")]
public class BoolToIntConverter : ValueConverterSOBase
{
    public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
    {
        if (value is bool b)
            return b ? 1 : 0;

        if (value != null && bool.TryParse(value.ToString(), out var parsed))
            return parsed ? 1 : 0;

        return 0;
    }

    public override object ConvertBack(object value, Type targetType, object parameter, IFormatProvider provider)
    {
        if (value is int i)
            return i != 0;

        if (value != null && int.TryParse(value.ToString(), out var parsed))
            return parsed != 0;

        return false;
    }
}