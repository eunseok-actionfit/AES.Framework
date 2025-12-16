using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;


namespace AES.Tools
{
    public sealed class IapFacade : IIap
    {
        private IapDatabase _db;
        private IIapPurchaseBackend _backend;

        // productKey -> localized price
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


        private void OnBackendPriceUpdated(string productId, string priceText)
        {
            try
            {
                Debug.Log($"[IAP] OnBackendPriceUpdated pid={productId}, price={priceText}");


                if (string.IsNullOrWhiteSpace(productId)) return;
                if (string.IsNullOrWhiteSpace(priceText)) return;

                if (_db != null && _db.TryResolveProductKeyByProductId(productId, out var productKey))
                {
                    _productKeyToPrice[productKey] = priceText;
                    PriceUpdatedByProductKey?.Invoke(productKey, priceText);
                    Debug.Log($"[IAP] Resolved productKey={productKey}");
                }
                else { Debug.LogError($"[IAP] Resolve FAILED productId={productId}"); }
            }
            catch (Exception e) { Debug.LogError($"[IAP] OnBackendPriceUpdated FAILED pid={productId}, price={priceText}\n{e}"); }
        }

        private void OnBackendConfirmed(Order order)
        {
            if (_db == null || order == null) return;

            // Order 안의 productId 목록을 꺼내서 productKey로 변환 후 브로드캐스트
            var info = order.Info;
            var purchased = info?.PurchasedProductInfo;
            if (purchased.Count == 0) return;

            foreach (var p in purchased)
            {
                var productId = p?.productId;
                if (string.IsNullOrWhiteSpace(productId)) continue;

                if (_db.TryResolveProductKeyByProductId(productId, out var productKey) &&
                    !string.IsNullOrWhiteSpace(productKey)) { PurchaseConfirmedByProductKey?.Invoke(productKey); }
            }
        }

        public bool TryGetLocalizedPriceByProductKey(string productKey, out string priceText)
        {
            priceText = null;

            if (string.IsNullOrWhiteSpace(productKey))
                return false;

            return _productKeyToPrice.TryGetValue(productKey, out priceText);
        }

        public UniTask PurchaseByProductKeyAsync(string productKey)
        {
            if (!IsReady) throw new InvalidOperationException("[IAP] Not ready.");

            if (!_db.TryResolveProductId(productKey, out var productId) || string.IsNullOrWhiteSpace(productId))
                throw new InvalidOperationException($"[IAP] ProductId not found for productKey: {productKey}");

            return _backend.PurchaseAsync(productId);
        }

        public UniTask RestoreAsync()
        {
            if (!IsReady) throw new InvalidOperationException("[IAP] Not ready.");
            return _backend.RestoreAsync();
        }
    }
}