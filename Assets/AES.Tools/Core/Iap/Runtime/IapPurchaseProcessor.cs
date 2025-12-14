using Cysharp.Threading.Tasks;

namespace AES.Tools
{
    public sealed class IapPurchaseProcessor : IIapPurchaseProcessor
    {
        private readonly IapDatabase _db;
        private readonly IIapRewardApplier _rewards;
        private readonly IIapTransactionStore _tx;
        private readonly IReceiptVerifier _verifier; // null 가능

        public IapPurchaseProcessor(
            IapDatabase db,
            IIapRewardApplier rewards,
            IIapTransactionStore tx,
            IReceiptVerifier verifier = null)
        {
            _db = db;
            _rewards = rewards;
            _tx = tx;
            _verifier = verifier;
        }

        public async UniTask<bool> ProcessAsync(IapPurchaseContext ctx)
        {
            if (ctx == null || string.IsNullOrWhiteSpace(ctx.StoreProductId))
                return false;

            // idempotent
            if (_tx != null && !string.IsNullOrEmpty(ctx.TransactionId) && _tx.IsProcessed(ctx.TransactionId))
                return true;

            if (!_db.TryResolveProductKeyBySku(ctx.StoreProductId, out var productKey))
                return false;

            // optional server verify
            if (_verifier != null)
            {
                var vr = await _verifier.VerifyAsync(ctx);
                if (vr == null || !vr.Ok)
                    return false;
            }

            // rewards
            var rewards = _db.GetRewards(productKey);
            if (rewards == null || rewards.Count == 0)
                return false;

            await _rewards.ApplyAsync(rewards);

            if (_tx != null && !string.IsNullOrEmpty(ctx.TransactionId))
                await _tx.MarkProcessedAsync(ctx.TransactionId);

            return true;
        }
    }
}