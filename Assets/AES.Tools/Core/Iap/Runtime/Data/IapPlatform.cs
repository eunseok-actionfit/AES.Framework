namespace AES.Tools
{
    public static class IapPlatform
    {
        public const string GP = "GP";
        public const string IOS = "IOS";

        public static string Current
        {
            get
            {
#if UNITY_IOS
                return IOS;
#else
                return GP;
#endif
            }
        }
    }
}
