using System;
using AES.Tools;
using UnityEngine;

[CreateAssetMenu(menuName = "AES/Converters/Enum ↔ Int")]
public class EnumToIntConverter : ValueConverterSOBase
{
    public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
    {
        if (value == null)
            return 0;

        if (value.GetType().IsEnum)
            return (int)value;

        int.TryParse(value.ToString(), out int parsed);
        return parsed;
    }

    public override object ConvertBack(object value, Type targetType, object parameter, IFormatProvider provider)
    {
        if (targetType.IsEnum)
        {
            if (value is int i)
                return Enum.ToObject(targetType, i);

            if (int.TryParse(value.ToString(), out var parsed))
                return Enum.ToObject(targetType, parsed);
        }

        return null;
    }
}