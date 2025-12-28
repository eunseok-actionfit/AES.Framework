using System;
using System.Text;
using UnityEngine;

namespace AES.Tools
{
    [CreateAssetMenu(
        fileName = "TimeSpanToPrettyTimerTextConverter",
        menuName = "AES/Converters/TimeSpan To Pretty Timer Text")]
    public sealed class TimeSpanToPrettyTimerTextConverter : ValueConverterSOBase
    {
        public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            if (value is not TimeSpan remain)
                return string.Empty;

            if (remain <= TimeSpan.Zero)
                return string.Empty;

            var sb = new StringBuilder();
            int shown = 0;

            void Append(int v, string suffix, bool pad)
            {
                if (v <= 0 || shown >= 2)
                    return;

                if (sb.Length > 0)
                    sb.Append(' ');

                sb.Append(pad ? v.ToString("00") : v.ToString());
                sb.Append(suffix);
                shown++;
            }

            // 상위 단위부터
            Append(remain.Days, "d", false);
            Append(remain.Hours, "h", shown > 0);
            Append(remain.Minutes, "m", shown > 0);
            Append(remain.Seconds, "s", shown > 0);

            return sb.ToString();
        }
    }
}