using UnityEngine;

[CreateAssetMenu(menuName = "App/Ads/Runtime Config", fileName = "AdsRuntimeConfig")]
public sealed class AdsRuntimeConfig : ScriptableObject
{
    [Header("전면 광고 최소 간격 (초)")]
    [Min(0f)]
    public float interstitialMinIntervalSeconds = 40f;

    [Header("세션당 전면 광고 최대 횟수")]
    [Min(0)]
    public int interstitialMaxPerSession = 10;

    [Header("런타임 기본 광고 OFF (예: QA 빌드)")]
    public bool runtimeAdsDisabled = false;

    [Header("테스트 기기 CSV (이름,디바이스ID[,adsDisabled,isTester])")]
    public TextAsset testDeviceCSV;
}