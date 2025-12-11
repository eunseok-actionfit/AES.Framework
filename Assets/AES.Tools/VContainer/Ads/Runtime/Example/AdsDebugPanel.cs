using UnityEngine;
using UnityEngine.UI;
using AES.Tools;
using AES.Tools.VContainer;


public sealed class AdsDebugPanel : MonoBehaviour
{
    [SerializeField] private Text interstitialCountText;
    [SerializeField] private Text rewardedCountText;
    [SerializeField] private Text rewardGrantedCountText;
    [SerializeField] private Text lastEventText;

    private int _interstitialCount;
    private int _rewardedCount;
    private int _rewardGrantedCount;

    private EventBinding<InterstitialFinishedEvent> _interstitialBinding;
    private EventBinding<RewardedFinishedEvent> _rewardedBinding;

    private void Awake()
    {
        _interstitialBinding = new EventBinding<InterstitialFinishedEvent>()
            .Add(OnInterstitialFinished)
            .Register();

        _rewardedBinding = new EventBinding<RewardedFinishedEvent>()
            .Add(OnRewardedFinished)
            .Register();

        RefreshUI();
    }

    private void OnDestroy()
    {
        _interstitialBinding?.Deregister();
        _rewardedBinding?.Deregister();
    }

    private void OnInterstitialFinished(InterstitialFinishedEvent e)
    {
        _interstitialCount++;
        lastEventText.text = $"Interstitial Finished (Succeeded={e.Succeeded})";
        RefreshUI();
    }

    private void OnRewardedFinished(RewardedFinishedEvent e)
    {
        _rewardedCount++;
        if (e.RewardGranted)
            _rewardGrantedCount++;

        lastEventText.text = $"Rewarded Finished (Succeeded={e.Succeeded}, Reward={e.RewardGranted})";
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (interstitialCountText != null)
            interstitialCountText.text = $"Interstitial: {_interstitialCount}";

        if (rewardedCountText != null)
            rewardedCountText.text = $"Rewarded: {_rewardedCount}";

        if (rewardGrantedCountText != null)
            rewardGrantedCountText.text = $"Reward Granted: {_rewardGrantedCount}";
    }
}