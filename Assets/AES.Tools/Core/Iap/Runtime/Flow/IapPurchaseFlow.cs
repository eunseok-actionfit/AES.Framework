using System;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    /// <summary>
    /// High-level purchase flow.
    ///  - UI buys by ProductKey
    ///  - Store callback gives SKU, flow maps -> ProductKey -> rewards -> apply
    /// This is the only place that knows about both store (SKU) and game rewards.
    /// </summary>
    public sealed class IapPurchaseFlow : IIapPurchaseProcessor
    {
        private readonly IapDatabase _db;
        private readonly IIapPurchaseBackend _backend;
        private readonly IIapRewardApplier _rewards;

        public IapPurchaseFlow(IapDatabase db, IIapPurchaseBackend backend, IIapRewardApplier rewards)
        {
            _db = db;
            _backend = backend;
            _rewards = rewards;
        }

        /// <summary>UI entry point: buy by ProductKey.</summary>
        public UniTask PurchaseByProductKeyAsync(string productKey)
        {
            if (!_db.TryResolveProductId(productKey, out var productId))
                throw new InvalidOperationException($"[IAP] ProductId not found. productKey={productKey} platform={IapPlatform.Current}");

            return _backend.PurchaseAsync(productId);
        }

        /// <summary>
        /// Store callback entry point: called by UnityIapService.
        /// Return true to confirm purchase; false to keep pending.
        /// </summary>
        public async UniTask<bool> ProcessAsync(IapPurchaseContext ctx)
        {
            if (ctx == null || string.IsNullOrWhiteSpace(ctx.StoreProductId))
                throw new ArgumentException("[IAP] Invalid purchase context");

            if (!_db.TryResolveProductKeyByProductId(ctx.StoreProductId, out var productKey))
                throw new InvalidOperationException($"[IAP] Cannot map ProductId->ProductKey. productId={ctx.StoreProductId} platform={IapPlatform.Current}");

            var rewards = _db.GetRewards(productKey);
            if (rewards.Count == 0)
                throw new InvalidOperationException($"[IAP] No rewards for productKey={productKey}");

            await _rewards.ApplyAsync(rewards);
            return true;
        }
    }
}
