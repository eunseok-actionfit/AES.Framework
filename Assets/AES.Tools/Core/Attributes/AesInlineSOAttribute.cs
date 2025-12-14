using UnityEngine;

namespace AES.Tools
{
    public sealed class AesInlineSOAttribute : PropertyAttribute
    {
        public readonly bool Foldout;
        public readonly bool DrawHeader;

        public AesInlineSOAttribute(bool foldout = true, bool drawHeader = true)
        {
            Foldout = foldout;
            DrawHeader = drawHeader;
        }
    }
}