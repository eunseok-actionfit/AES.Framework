using System;
using Cysharp.Threading.Tasks;

namespace AES.Tools
{
    public sealed class IapFacade : IIap
    {
        private IapDatabase _db;
        private IIapPurchaseBackend _backend;

        public bool IsReady => _db != null && _backend != null;

        internal void SetReady(IapDatabase db, IIapPurchaseBackend backend)
        {
            _db = db;
            _backend = backend;
        }

        public UniTask PurchaseByProductKeyAsync(string productKey)
        {
            if (!IsReady) throw new InvalidOperationException("[IAP] Not ready.");

            if (!_db.TryResolveSku(productKey, out var sku) || string.IsNullOrWhiteSpace(sku))
                throw new InvalidOperationException($"[IAP] SKU not found for productKey: {productKey}");

            return _backend.PurchaseAsync(sku);
        }

        public UniTask RestoreAsync()
        {
            if (!IsReady) throw new InvalidOperationException("[IAP] Not ready.");
            return _backend.RestoreAsync();
        }
    }
}