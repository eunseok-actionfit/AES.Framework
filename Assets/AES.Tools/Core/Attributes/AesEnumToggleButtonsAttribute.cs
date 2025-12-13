using System;
using UnityEngine;


namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AesEnumToggleButtonsAttribute : PropertyAttribute
    {
        public AesEnumToggleButtonsAttribute() { }
    }
}