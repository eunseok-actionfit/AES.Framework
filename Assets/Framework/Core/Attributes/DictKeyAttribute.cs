using System;


namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DictKeyAttribute : Attribute
    {
        public object DefaultValue { get; }

        public DictKeyAttribute(object defaultValue = null)
        {
            DefaultValue = defaultValue;
        }
    }
}


