using System;
using UnityEngine;

namespace AES.Tools
{
    /// <summary>
    /// 읽기 전용 표시용 Attribute.
    /// Odin 유무에 따라 Drawer에서만 분기해서 처리.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AesReadOnlyAttribute : PropertyAttribute
    { }
}