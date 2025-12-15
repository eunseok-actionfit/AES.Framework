using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace AES.Tools
{
    public sealed class IapFacade : IIap
    {
        private IapDatabase _db;
        private IIapPurchaseBackend _backend;

        // sku/productKey -> localized price
        private readonly Dictionary<string, string> _skuToPrice = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _productKeyToPrice = new(StringComparer.Ordinal);

        public bool IsReady => _db != null && _backend != null;

        public IapDatabase Database => _db;

        public event Action Ready;
        public event Action<string, string> PriceUpdatedByProductKey;

        internal void SetReady(IapDatabase db, IIapPurchaseBackend backend)
        {
            // 기존 구독 해제
            if (_backend is UnityIapBackend prevBackend)
                prevBackend.PriceUpdated -= OnBackendPriceUpdated;

            _db = db;
            _backend = backend;

            if (_backend is UnityIapBackend curBackend)
                curBackend.PriceUpdated += OnBackendPriceUpdated;

            Ready?.Invoke();
        }

        private void OnBackendPriceUpdated(string sku, string priceText)
        {
            if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(priceText))
                return;

            _skuToPrice[sku] = priceText;

            if (_db != null && _db.TryResolveProductKeyBySku(sku, out var productKey) && !string.IsNullOrWhiteSpace(productKey))
            {
                _productKeyToPrice[productKey] = priceText;
                PriceUpdatedByProductKey?.Invoke(productKey, priceText);
            }
        }

        public bool TryGetLocalizedPriceByProductKey(string productKey, out string priceText)
        {
            priceText = null;
            if (string.IsNullOrWhiteSpace(productKey))
                return false;

            return _productKeyToPrice.TryGetValue(productKey, out priceText) && !string.IsNullOrWhiteSpace(priceText);
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
