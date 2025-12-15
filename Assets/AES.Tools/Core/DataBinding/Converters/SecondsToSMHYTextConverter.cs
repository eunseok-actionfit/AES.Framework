using System;
using System.Globalization;
using UnityEngine;

namespace AES.Tools
{
    [CreateAssetMenu(
        fileName = "SecondsToSMHYTextConverter",
        menuName = "AES/Converters/Seconds To SMHY Text")]
    public sealed class SecondsToSMHYTextConverter : ValueConverterSOBase
    {
        [SerializeField] bool includeZeroUnits;

        public override object Convert(
            object value,
            Type targetType,
            object parameter,
            IFormatProvider provider)
        {
            if (value == null)
                return string.Empty;

            if (value is TimeSpan ts)
                return ts.ToSMHY(includeZeroUnits);

            try
            {
                // int / long / float / double / string 숫자 전부 커버
                double seconds = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return seconds.ToSMHY(includeZeroUnits);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}