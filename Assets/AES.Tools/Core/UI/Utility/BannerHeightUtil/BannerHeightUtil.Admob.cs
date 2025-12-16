namespace AES.Tools.UI.Utility
{
    public static partial class BannerHeightUtil
    {
#if AESFW_ADS_ADMOB
        public static int GetAdmobAdaptiveBannerHeightPx()
        {
            var adaptiveSize =
                GoogleMobileAds.Api.AdSize
                    .GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
                        GoogleMobileAds.Api.AdSize.FullWidth
                    );

            float heightDp = adaptiveSize.Height;
            return DpToPx(heightDp);
        }
#else
        public static int GetAdmobAdaptiveBannerHeightPx() => GetDefaultBannerHeightPx();
#endif
    }
}