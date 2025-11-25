using System.Diagnostics;
using UnityEditor;
using UnityEngine;


namespace AES.Tools
{
    [Conditional("UNITY_EDITOR")]
    public class InformationAttribute : PropertyAttribute
    {
        public enum InformationType
        {
            Error, Info, None,
            Warning
        }

		#if UNITY_EDITOR
        public readonly string Message;
        public readonly MessageType Type;
        public readonly bool MessageAfterProperty;

        public InformationAttribute(string message, InformationType type, bool messageAfterProperty)
        {
            Message = message;

            if (type == InformationType.Error) { Type = MessageType.Error; }

            if (type == InformationType.Info) { Type = MessageType.Info; }

            if (type == InformationType.Warning) { Type = MessageType.Warning; }

            if (type == InformationType.None) { Type = MessageType.None; }

            MessageAfterProperty = messageAfterProperty;
        }
		#else
		public InformationAttribute(string message, InformationType type, bool messageAfterProperty)
		{

		}
		#endif
    }
}