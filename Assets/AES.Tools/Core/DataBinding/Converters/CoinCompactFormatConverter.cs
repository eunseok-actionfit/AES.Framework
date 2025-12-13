using System;
using System.Globalization;
using UnityEngine;


namespace AES.Tools
{
    [CreateAssetMenu(menuName = "AES/ValueConverters/Coin Compact Format")]
    public class CoinCompactFormatConverter : ValueConverterSOBase
    {
        [SerializeField] int decimalDigits = 1;

        public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            if (value == null)
                return string.Empty;

            double number;
            try
            {
                number = System.Convert.ToDouble(value, provider ?? CultureInfo.InvariantCulture);
            }
            catch
            {
                return value.ToString();
            }

            return number.ToCompact(decimalDigits);
        }
    }
}