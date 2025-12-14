using UnityEngine;

namespace AES.Tools
{
    public abstract class AesConditionAttribute : PropertyAttribute
    {
        public readonly string Member;
        public readonly bool Invert;

        protected AesConditionAttribute(string member, bool invert = false)
        {
            Member = member;
            Invert = invert;
        }
    }

    public sealed class AesShowIfAttribute : AesConditionAttribute
    {
        public AesShowIfAttribute(string member, bool invert = false)
            : base(member, invert) { }
    }

    public sealed class AesEnableIfAttribute : AesConditionAttribute
    {
        public AesEnableIfAttribute(string member, bool invert = false)
            : base(member, invert) { }
    }
}