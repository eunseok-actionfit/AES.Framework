using System;
using UnityEngine;

namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class AesLabelTextAttribute : PropertyAttribute
    {
        public readonly string Text;

        public AesLabelTextAttribute(string text)
        {
            Text = text;
        }
    }
}