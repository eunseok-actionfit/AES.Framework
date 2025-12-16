// using UnityEngine;
// using UnityEngine.UI;
// using AES.Tools;
// using System;
// using AES.Tools.VContainer;
// using Cysharp.Threading.Tasks;
//
//
// public sealed class AdsDemoUI : MonoBehaviour
// {
//     [Header("옵션 UI")]
//     [SerializeField] private Toggle disableAdsToggle;
//     
//     [Header("로그 표시용")]
//     [SerializeField] private Text logText;
//
//     private EventBinding<AdShowingStateChangedEvent> _adShowingBinding;
//
//     private void Awake()
//     {
//         // 광고 표시/종료 이벤트 구독
//         _adShowingBinding = new EventBinding<AdShowingStateChangedEvent>()
//             .Add(OnAdShowingStateChanged)
//             .Register();
//
//         if (disableAdsToggle != null)
//         {
//             disableAdsToggle.onValueChanged.AddListener(OnDisableAdsToggleChanged);
//         }
//
//         Log("[AdsDemoUI] Ready.");
//     }
//
//     private void OnDestroy()
//     {
//         _adShowingBinding?.Deregister();
//         _adShowingBinding = null;
//
//         if (disableAdsToggle != null)
//         {
//             disableAdsToggle.onValueChanged.RemoveListener(OnDisableAdsToggleChanged);
//         }
//     }
//
//     private void OnDisableAdsToggleChanged(bool disabled)
//     {
//         ADS.SetRuntimeDisabled(disabled);
//         Log($"[AdsDemoUI] Runtime Ads Disabled = {disabled}");
//     }
//
//     private void OnAdShowingStateChanged(AdShowingStateChangedEvent e)
//     {
//         Log($"[AdsDemoUI] AdShowing = {e.IsShowing}, Placement = {e.Placement}");
//     }
//
//     private void Log(string msg)
//     {
//         Debug.Log(msg);
//         if (logText == null) return;
//
//         logText.text = $"{DateTime.Now:HH:mm:ss} {msg}\n" + logText.text;
//     }
//
//     //========================
//     // 버튼용 핸들러들
//     //========================
//
//     public void OnClick_ShowInterstitial()
//     {
//         ADS.ShowInterstitial();
//         Log("[AdsDemoUI] ShowInterstitial()");
//     }
//
//     public void OnClick_ShowRewarded()
//     {
//         ADS.ShowRewarded(() =>
//         {
//             Log("[AdsDemoUI] Reward callback → 보상 지급");
//             // 실제 보상 지급 로직은 게임 쪽에서 추가
//         });
//     }
//
//     public void OnClick_ShowBanner()
//     {
//         ADS.ShowBanner();
//         Log("[AdsDemoUI] ShowBanner()");
//     }
//
//     public void OnClick_HideBanner()
//     {
//         ADS.HideBanner();
//         Log("[AdsDemoUI] HideBanner()");
//     }
//
//     // Async 버전 API를 구현했다면, 이런 식으로도 연결 가능 (나중에 도입 시 사용)
//     
//     public async void OnClick_ShowInterstitialAsync()
//     {
//         bool ok = await ADS.ShowInterstitialAsync(this.GetCancellationTokenOnDestroy());
//         Log($"[AdsDemoUI] ShowInterstitialAsync() Completed. Result = {ok}");
//     }
//
//     public async void OnClick_ShowRewardedAsync()
//     {
//         bool reward = await ADS.ShowRewardedAsync(this.GetCancellationTokenOnDestroy());
//         Log($"[AdsDemoUI] ShowRewardedAsync() Completed. Reward = {reward}");
//     }
// }
