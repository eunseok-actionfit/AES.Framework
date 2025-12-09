using UnityEngine;


namespace AES.Tools.VContainer
{
    public class AdsTestMono:MonoBehaviour
    {
        void Start()
        {
            ADS.ShowBanner();
        }

        public void ShowBanner()
        {
            ADS.ShowBanner();
        }
        
        // 전투진입
        public void HideBanner()
        {
            ADS.HideBanner();
        }

        // 스테이지 클리어
        public void ShowInterstitial()
        {
            ADS.ShowInterstitial();
        }

        // 리워드 버튼
        public void ShowRewarded()
        {
            ADS.ShowRewarded(() =>
            {
               Debug.Log("ShownRewarded");
            });
        }

    }
}


