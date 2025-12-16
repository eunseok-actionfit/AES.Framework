using System;


namespace AES.Tools.VContainer
{
    [Serializable]
    public sealed class AdsRuntimeSettings
    {
        public float interstitialMinIntervalSeconds = 40f;
        public int interstitialMaxPerSession = 10;
        public bool runtimeAdsDisabled = false;
        
        public float appOpenMinBackgroundSeconds = 60f;   // 짧은 복귀(결제/UI) 차단
        public float appOpenBlockAfterSensitiveSeconds = 10f; // 결제/권한 직후 차단
        
        public float appOpenResumeCooldownSeconds = 180f;
    }
}