using System;
using UnityEngine;

namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class AesGUIColorAttribute : PropertyAttribute
    {
        /// <summary>
        /// Color / Color32 를 반환하는 필드/프로퍼티/메서드 이름 또는 @표현식
        /// </summary>
        public readonly string ColorSource;

        public AesGUIColorAttribute(string colorSource)
        {
            ColorSource = colorSource;
        }
    }
}