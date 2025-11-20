using System;
using AES.Tools.Core.Utility;


namespace AES.Tools.Core
{
    [Serializable]
    public readonly struct UIWindowKey : IEquatable<UIWindowKey>
    {
        private readonly long _value;
        public long Value => _value;

        public UIWindowKey(long value)
        {
            _value = value;
        }

        public static UIWindowKey FromEnum<TEnum>(TEnum id) where TEnum : Enum
        {
            var key = UIEnumKeyUtility.ComputeEnumHash(id);
            return new UIWindowKey(key);
        }

        public bool Equals(UIWindowKey other) => _value == other._value;

        public override bool Equals(object obj) =>
            obj is UIWindowKey other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(UIWindowKey left, UIWindowKey right) =>
            left.Equals(right);

        public static bool operator !=(UIWindowKey left, UIWindowKey right) =>
            !left.Equals(right);

        public override string ToString() => $"UIWindowKey({_value})";
    }
}