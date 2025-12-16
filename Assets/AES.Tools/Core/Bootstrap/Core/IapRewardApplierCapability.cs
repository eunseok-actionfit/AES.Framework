
using AES.Tools.VContainer.Bootstrap.Framework;


namespace AES.Tools
{
    public sealed class IapRewardApplierCapability : IIapRewardApplierCapability
    {
        public IIapRewardApplier RewardApplier { get; set; }

        public IapRewardApplierCapability(IIapRewardApplier rewardApplier = null)
        {
            RewardApplier = rewardApplier;
        }
    }
}


