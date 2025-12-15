using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

        public UnityIapBackend(
            List<ProductDefinition> products,
            IIapPurchaseProcessor processor,
            string storeName = null)
        {
            _store = UnityIAPServices.StoreController(storeName);
            _processor = processor;

            _store.OnPurchasePending += HandlePending;
            _store.OnPurchaseConfirmed += o => OnConfirmed?.Invoke(o);
            _store.OnPurchaseFailed += f => OnFailed?.Invoke(f);
            _store.OnPurchaseDeferred += d => OnDeferred?.Invoke(d);

            _store.OnProductsFetched += OnProductsFetched;
            
            _store.FetchProducts(products);
        }
        

        public UniTask InitializeAsync()
            => _store.Connect().AsUniTask();

        public UniTask PurchaseAsync(string sku)
        {
            _store.PurchaseProduct(sku);
            return UniTask.CompletedTask;
        }

        public UniTask RestoreAsync()
        {
#if UNITY_IOS
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

                var ok = await _processor.ProcessAsync(ctx);
                if (!ok)
                    return; // 하나라도 보류면 전체 보류
            }

            _store.ConfirmPurchase(order);
        }
        
        private void OnProductsFetched(List<Product> products)
        {
            if (products == null) return;

            foreach (var p in products)
            {
                if (p == null) continue;

                var sku = p.definition?.id;
                if (string.IsNullOrEmpty(sku)) continue;

                // Unity IAP가 지역/통화에 맞춰 내려주는 문자열
                var priceText = p.metadata?.localizedPriceString ?? string.Empty;

                if (!string.IsNullOrEmpty(priceText))
                    PriceUpdated?.Invoke(sku, priceText);
            }
        }
    }
}
