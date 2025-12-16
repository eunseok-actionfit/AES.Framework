using UnityEngine;

namespace AES.Tools.UI.Utility
{
    public static partial class BannerHeightUtil
    {
        private const int DEFAULT_BANNER_DP = 50;

        public static int DpToPx(float dp)
        {
            float dpi = Screen.dpi;
            if (dpi <= 0f) dpi = 160f;
            return Mathf.RoundToInt(dp * (dpi / 160f));
        }

        public static int GetDefaultBannerHeightPx() => DpToPx(DEFAULT_BANNER_DP);
    }
}