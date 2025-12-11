using UnityEngine;
using Cysharp.Threading.Tasks;
using AES.Tools;                // EventBus
using System.Threading;
using AES.Tools.VContainer;


public sealed class AdsUsageExample : MonoBehaviour
{
    private CancellationTokenSource _cts;

    CompositeDisposable _disposables = new();
    
    private void Start()
    {
        _cts = new CancellationTokenSource();

        // 광고 표시/종료 시점에 UI/입력제어 처리
        new EventBinding<AdShowingStateChangedEvent>()
            .Register(OnAdShowingChanged)
            .AddTo(_disposables);
        
        Debug.Log("[AdsExample] Example started.");
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        _disposables.Dispose();
    }

    private void OnAdShowingChanged(AdShowingStateChangedEvent e)
    {
        if (e.IsShowing)
        {
            Debug.Log($"[AdsExample] 광고 표시됨({e.Placement}) → UI 잠금/일시정지 등 적용");

            // 예: UIManager.Instance.SetInteractable(false);
            // 예: GameManager.Instance.PauseGame();
        }
        else
        {
            Debug.Log($"[AdsExample] 광고 종료({e.Placement}) → UI 복구");

            // 예: UIManager.Instance.SetInteractable(true);
            // 예: GameManager.Instance.ResumeGame();
        }
    }

    //======================================================
    // 1. 버튼에서 간단히 "전면 광고"와 "보상 광고" 보여주기
    //======================================================

    public void OnClick_ShowInterstitial()
    {
        ADS.ShowInterstitial();
    }

    public void OnClick_ShowRewarded()
    {
        ADS.ShowRewarded(() =>
        {
            Debug.Log("[AdsExample] 보상 지급!");
            // 코인 지급 등 처리
        });
    }

    //======================================================
    // 2. Async 패턴 사용 (UX 완전 관리)
    //======================================================

    public async void OnClick_ShowInterstitialAsync()
    {
        Debug.Log("[AdsExample] 전면 광고 시작 요청");

        bool result = await ADS.ShowInterstitialAsync();

        Debug.Log($"[AdsExample] 전면 광고 완료. Result = {result}");
        
        if (!result)
        {
            // 실패/취소
            // 예: UIManager.Instance.ShowToast("광고 준비되지 않음");
        }
    }

    public async void OnClick_ShowRewardedAsync()
    {
        Debug.Log("[AdsExample] 보상 광고 시작 요청");

        bool reward = await ADS.ShowRewardedAsync(_cts.Token);

        Debug.Log($"[AdsExample] 보상 광고 완료. Reward = {reward}");

        if (reward)
        {
            Debug.Log("[AdsExample] 보상 지급!");
            // 코인 지급
        }
        else
        {
            Debug.Log("[AdsExample] 보상 실패/취소");
            // 사용자 취소 / 광고 실패 등의 경우
        }
    }

    //======================================================
    // 3. 배너 제어 예시
    //======================================================

    public void OnClick_ShowBanner()
    {
        ADS.ShowBanner();
    }

    public void OnClick_HideBanner()
    {
        Debug.Log("[TEST] HideBanner 버튼 눌림");
        ADS.HideBanner();
    }

    //======================================================
    // 4. 런타임 광고 OFF (IAP 광고제거 등)
    //======================================================

    public void OnClick_DisableAds()
    {
        ADS.SetRuntimeDisabled(true);
        Debug.Log("[AdsExample] 런타임 광고 OFF");
    }

    public void OnClick_EnableAds()
    {
        ADS.SetRuntimeDisabled(false);
        Debug.Log("[AdsExample] 런타임 광고 ON");
    }
}
