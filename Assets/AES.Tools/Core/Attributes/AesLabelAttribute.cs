using UnityEngine;

namespace AES.Tools
{
    public sealed class AesLabelAttribute : PropertyAttribute
    {
        public readonly string Text;
        public AesLabelAttribute(string text) => Text = text;
    }
}