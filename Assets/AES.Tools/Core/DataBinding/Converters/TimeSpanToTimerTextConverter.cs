using System;
using UnityEngine;
using AES.Tools;

[CreateAssetMenu(
    fileName = "TimeSpanToTimerTextConverter",
    menuName = "AES/Converters/TimeSpan To Timer Text")]
public sealed class TimeSpanToTimerTextConverter : ValueConverterSOBase   
{
    public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
    {
        if (value is not TimeSpan remain)
            return string.Empty;

        if (remain <= TimeSpan.Zero)
            return "MAX";

        // MM:SS 형태로 포맷
        return $"{remain.Minutes:00} : {remain.Seconds:00}";
    }
}