using System;
using System.Globalization;
using UnityEngine;


namespace AES.Tools
{
    [CreateAssetMenu(menuName = "AES/ValueConverters/Number Format")]
    public class NumberFormatConverter : ValueConverterSOBase
    {
        [SerializeField] string format = "N0";
        [SerializeField] bool useInvariantCulture;

        public override object Convert(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            // parameter 가 string 이면 format override 용도로 사용
            var fmt = parameter as string ?? format;

            var culture = useInvariantCulture
                ? CultureInfo.InvariantCulture
                : (provider ?? CultureInfo.CurrentCulture);

            // 올바른 TextFormatHelper 호출
            return TextFormatHelper.Format(value, true, fmt, culture);
        }
    }
}