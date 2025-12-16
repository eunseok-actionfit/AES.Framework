namespace AES.Tools.UI.Utility
{
    public static partial class BannerHeightUtil
    {
#if AESFW_ADS_MAX
        public static int GetMaxAdaptiveBannerHeightPx()
        {
            float heightDp = MaxSdkUtils.GetAdaptiveBannerHeight(0);
            return DpToPx(heightDp);
        }
#else
        public static int GetMaxAdaptiveBannerHeightPx() => GetDefaultBannerHeightPx();
#endif
    }
}