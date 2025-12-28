using System;
using UnityEngine;

namespace AES.Tools
{
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

            if (remain.Days > 0)
                return $"{remain.Days:00}:{remain.Hours:00}:{remain.Minutes:00}:{remain.Seconds:00}";

            if (remain.Hours > 0)
                return $"{remain.Hours:00}:{remain.Minutes:00}:{remain.Seconds:00}";

            if (remain.Minutes > 0)
                return $"{remain.Minutes:00}:{remain.Seconds:00}";

            return $"{remain.Seconds:00}";
        }
    }
}