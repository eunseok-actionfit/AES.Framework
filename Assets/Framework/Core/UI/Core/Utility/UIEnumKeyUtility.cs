using System;


namespace AES.Tools.Core
{
    public static class UIEnumKeyUtility
    {
        // enum 타입 메타데이터 + 값으로 유니크 키 생성
        public static long ComputeEnumHash<TEnum>(TEnum value) where TEnum : Enum
        {
            var enumType  = typeof(TEnum);
            var typeToken = enumType.MetadataToken;
            var v         = Convert.ToInt32(value);

            unchecked
            {
                return ((long)typeToken << 32) | (uint)v;
            }
        }
    }
}