using System;
using UnityEngine;


namespace AES.Tools.Editor
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AesEnumToggleButtonsAttribute : PropertyAttribute
    {
        public AesEnumToggleButtonsAttribute() { }
    }
}