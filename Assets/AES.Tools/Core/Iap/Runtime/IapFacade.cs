using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Purchasing;


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
        public event Action<string> PurchaseConfirmedByProductKey;

        internal void SetReady(IapDatabase db, IIapPurchaseBackend backend)
        {
            if (_backend is UnityIapBackend prev)
            {
                prev.PriceUpdated -= OnBackendPriceUpdated;
                prev.OnConfirmed -= OnBackendConfirmed; // 추가
            }

            _db = db;
            _backend = backend;

            if (_backend is UnityIapBackend cur)
            {
                cur.PriceUpdated += OnBackendPriceUpdated;
                cur.OnConfirmed += OnBackendConfirmed; // 추가
            }

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
        
        private void OnBackendConfirmed(Order order)
        {
            if (_db == null || order == null) return;

            // Order 안의 productId(sku) 목록을 꺼내서 productKey로 변환 후 브로드캐스트
            var info = order.Info;
            var purchased = info?.PurchasedProductInfo;
            if (purchased.Count == 0) return;

            foreach (var p in purchased)
            {
                var sku = p?.productId;
                if (string.IsNullOrWhiteSpace(sku)) continue;

                if (_db.TryResolveProductKeyBySku(sku, out var productKey) &&
                    !string.IsNullOrWhiteSpace(productKey))
                {
                    PurchaseConfirmedByProductKey?.Invoke(productKey);
                }
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
