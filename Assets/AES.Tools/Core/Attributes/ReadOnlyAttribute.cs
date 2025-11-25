using System;
using System.Diagnostics;
using UnityEngine;


namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
    [Conditional("UNITY_EDITOR")]
    public class ReadOnlyAttribute : PropertyAttribute { }
}


