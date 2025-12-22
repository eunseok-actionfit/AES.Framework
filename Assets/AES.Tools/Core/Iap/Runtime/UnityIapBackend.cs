#if AESFW_IAP
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Singular;
using UnityEngine;
using UnityEngine.Purchasing;

namespace AES.Tools
{
    public sealed class UnityIapBackend : IIapPurchaseBackend
    {
        private readonly StoreController _store;
        private readonly IIapPurchaseProcessor _processor;

        public event Action<Order> OnConfirmed;
        public event Action<FailedOrder> OnFailed;
        public event Action<DeferredOrder> OnDeferred;
        
        public event Action<string, string> PriceUpdated; 
        private readonly List<ProductDefinition> _products;

        public UnityIapBackend(
            List<ProductDefinition> products,
            IIapPurchaseProcessor processor,
            string storeName = null)
        {
            _store = UnityIAPServices.StoreController(storeName);
            _products = products;
            _processor = processor;

            _store.OnPurchasePending += HandlePending;
            _store.OnPurchaseConfirmed += o =>
            {
                VContainer.ADS.NotifySensitiveFlowEnded(); 
                OnConfirmed?.Invoke(o);
            };

            _store.OnPurchaseFailed += f =>
            {
                VContainer.ADS.NotifySensitiveFlowEnded(); 
                OnFailed?.Invoke(f);
            };

            _store.OnPurchaseDeferred += d =>
            {
                VContainer.ADS.NotifySensitiveFlowEnded(); 
                OnDeferred?.Invoke(d);
            };

            _store.OnProductsFetched += OnProductsFetched;
        }
        
        private bool _fetched;
        public async UniTask InitializeAsync()
        {
            await _store.Connect().AsUniTask();
            
            if (!_fetched)
            {
                _fetched = true;
                _store.FetchProducts(_products);
            }
        }

        public UniTask PurchaseAsync(string productId)
        {
            _store.PurchaseProduct(productId);
            return UniTask.CompletedTask;
        }

        public UniTask RestoreAsync()
        {
#if UNITY_IOS
    VContainer.ADS.NotifySensitiveFlowStarted();
    _store.RestoreTransactions(null);
#endif
            return UniTask.CompletedTask;
        }

        private async void HandlePending(PendingOrder order)
        {
            var info = order.Info;
            var purchased = info.PurchasedProductInfo;
            if (purchased.Count == 0)
                return;

            foreach (var p in purchased)
            {
                var ctx = new IapPurchaseContext
                {
                    StoreProductId = p.productId,
                    TransactionId = info.TransactionID,
                    UnityReceipt = info.Receipt,
                };

                Debug.Log($"[IAP] Pending productId={p.productId}, tx={info.TransactionID}");

                var ok = await _processor.ProcessAsync(ctx);
                Debug.Log($"[IAP] Process result={ok} for productId={p.productId}");
                if (!ok)
                    return;
            }
            
            SingularSDK.InAppPurchase(order);

            _store.ConfirmPurchase(order);
        }

        
        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"[IAP] OnProductsFetched count={(products?.Count ?? -1)}");

            if (products == null) return;

            foreach (var p in products)
            {
                if (p == null) continue;

                var productId = p.definition?.id;
                if (string.IsNullOrEmpty(productId)) continue;

                // Unity IAP가 지역/통화에 맞춰 내려주는 문자열
                var priceText = p.metadata?.localizedPriceString ?? string.Empty;
                
                Debug.Log($"[IAP] Fetched id={productId}, price={priceText}");

                if (!string.IsNullOrEmpty(priceText))
                    PriceUpdated?.Invoke(productId, priceText);
            }
        }
    }
}
#endif