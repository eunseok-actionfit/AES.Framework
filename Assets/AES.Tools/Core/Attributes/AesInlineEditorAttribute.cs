using System;
using UnityEngine;

namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class AesInlineEditorAttribute : PropertyAttribute
    {
        public readonly bool DrawHeader;
        public readonly bool DrawPreview;

        public AesInlineEditorAttribute(bool drawHeader = true, bool drawPreview = false)
        {
            DrawHeader = drawHeader;
            DrawPreview = drawPreview;
        }
    }
}