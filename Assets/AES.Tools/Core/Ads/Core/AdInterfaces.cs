using System;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools.VContainer
{
    public interface IAdsService
    {
        void Configure(AdsProfile profile);

        void ShowBanner();
        void HideBanner();

        void ShowInterstitial();
        bool IsReadyInterstitial { get; }

        void ShowRewarded(Action onReward);
        bool IsReadyRewarded { get; }
        
        void SetRuntimeDisabled(bool disabled);
        
        UniTask<bool> ShowInterstitialAsync(CancellationToken ct = default);
        UniTask<bool> ShowRewardedAsync(CancellationToken ct = default);
        
        bool IsReadyAppOpen { get; }

        /// <summary>
        /// AppOpen이 준비될 때까지 최대 maxWaitSeconds 동안 기다립니다.
        /// 준비되면 true, 아니면 false.
        /// </summary>
        UniTask<bool> WaitForAppOpenReadyAsync(float maxWaitSeconds, CancellationToken ct = default);

        /// <summary>
        /// 준비되어 있으면 AppOpen을 한 번 보여주고 true, 아니면 false.
        /// </summary>
        bool TryShowAppOpen();

    }

    public interface IAppOpenAdService
    {
        void Initialize();
        void Load();
        bool IsReady { get; }
        void ShowIfReady();
    }

    public interface IInterstitialAdService
    {
        void Initialize();
        void Load();
        bool IsReady { get; }
        bool Show();
    }

    public interface IRewardedAdService
    {
        void Initialize();
        void Load();
        bool IsReady { get; }
        bool Show(Action onReward);
    }

    public interface IBannerAdService
    {
        void Initialize();
        void Hide();
        void Show();
    }
}