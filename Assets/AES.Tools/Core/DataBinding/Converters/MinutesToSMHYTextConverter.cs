using System;
using System.Globalization;
using UnityEngine;

namespace AES.Tools
{
    [CreateAssetMenu(
        fileName = "MinutesToSMHYTextConverter",
        menuName = "AES/Converters/Minutes To SMHY Text")]
    public sealed class MinutesToSMHYTextConverter : ValueConverterSOBase
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
                double minutes = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return minutes.MinutesToSMHY(includeZeroUnits);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}