using System;
using UnityEngine;


namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class AesInformationAttribute : PropertyAttribute
    {
        public readonly string Message;
        public readonly InfoType Type;
        public readonly bool MessageAfterProperty;

        public enum InfoType
        {
            Error,
            Info,
            Warning,
            None
        }

        public AesInformationAttribute(
            string message, 
            InfoType type = InfoType.Info, 
            bool afterProperty = false)
        {
            Message = message;
            MessageAfterProperty = afterProperty;
            Type = type;
        }
    }
}