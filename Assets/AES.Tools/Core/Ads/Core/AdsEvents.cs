namespace AES.Tools.VContainer
{
    public struct AdShowingStateChangedEvent : IEvent
    {
        public readonly bool IsShowing;
        public readonly AdPlacementType Placement;

        public AdShowingStateChangedEvent(bool isShowing, AdPlacementType placement)
        {
            IsShowing = isShowing;
            Placement = placement;
        }
    }

    public struct InterstitialFinishedEvent : IEvent
    {
        // true: 정상 표시 후 닫힘, false: 표시 실패 등
        public readonly bool Succeeded;

        public InterstitialFinishedEvent(bool succeeded)
        {
            Succeeded = succeeded;
        }
    }

    public struct RewardedFinishedEvent : IEvent
    {
        // 광고 자체는 끝났는지
        public readonly bool Succeeded;
        // 보상을 실제로 지급해야 하는지
        public readonly bool RewardGranted;

        public RewardedFinishedEvent(bool succeeded, bool rewardGranted)
        {
            Succeeded = succeeded;
            RewardGranted = rewardGranted;
        }
    }

}