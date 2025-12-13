using GoogleMobileAds.Api;
using UnityEngine;


namespace AES.Tools.UI.Utility
{
    public static class BannerHeightUtil
    {
        // 일반 고정 배너 기본값 (50dp)
        private const int DEFAULT_BANNER_DP = 50;

        /// <summary>
        /// dp → px 변환
        /// </summary>
        public static int DpToPx(float dp)
        {
            float dpi = Screen.dpi;
            if (dpi <= 0f)
            {
                // DPI 못 가져오는 기기 대비 기본 mdpi(160) 가정
                dpi = 160f;
            }

            return Mathf.RoundToInt(dp * (dpi / 160f));
        }

        /// <summary>
        /// 그냥 50dp 고정 배너 높이(px) (fallback용)
        /// </summary>
        public static int GetDefaultBannerHeightPx()
        {
            return DpToPx(DEFAULT_BANNER_DP);
        }

        /// <summary>
        /// AdMob Adaptive Banner 높이(px)
        /// </summary>
        public static int GetAdmobAdaptiveBannerHeightPx()
        {
            // 가로폭: 풀 폭 기준
            AdSize adaptiveSize =
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(
                    AdSize.FullWidth
                );

            // Unity용 GoogleMobileAds의 AdSize.Height는 dp 단위
            float heightDp = adaptiveSize.Height;

            return DpToPx(heightDp);
        }

        /// <summary>
        /// MAX Adaptive Banner 높이(px)
        /// </summary>
        public static int GetMaxAdaptiveBannerHeightPx()
        {
            // width = 0 → 디바이스 전체 폭 기준 (MAX 공식 샘플 패턴과 동일)
            float heightDp = MaxSdkUtils.GetAdaptiveBannerHeight(0);

            return DpToPx(heightDp);
        }
    }
}