using System;
using UnityEngine;

namespace AES.Tools
{
    public interface IValueConverter
    {
        object Convert(object value, Type targetType, object parameter, IFormatProvider provider);
        object ConvertBack(object value, Type targetType, object parameter, IFormatProvider provider);
    }

    public abstract class ValueConverterSOBase : ScriptableObject, IValueConverter
    {
        public abstract object Convert(object value, Type targetType, object parameter, IFormatProvider provider);

        // 필요 없으면 미구현으로 둔다.
        public virtual object ConvertBack(object value, Type targetType, object parameter, IFormatProvider provider)
        {
            throw new NotSupportedException($"{GetType().Name} does not support ConvertBack.");
        }
    }
}