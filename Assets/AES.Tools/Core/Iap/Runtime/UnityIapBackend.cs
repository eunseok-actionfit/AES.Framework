#if AESFW_IAP
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

#if AESFW_ANALYTICS_SINGULAR
using Singular;
#endif

namespace AES.Tools
{
    public sealed class UnityIapBackend : IIapPurchaseBackend
    {
        private readonly StoreController _store;
        private readonly IIapPurchaseProcessor _processor;
        private readonly List<ProductDefinition> _products;

        public event Action<Order> OnConfirmed;
        public event Action<FailedOrder> OnFailed;
        public event Action<DeferredOrder> OnDeferred;

        /// <summary>영구 상품 소유 감지 (productId, receipt)</summary>
        public static event Action<string, string> OwnedNonConsumableFound;

        public event Action<string, string> PriceUpdated;

        private bool _initialized;

        private readonly UniTaskCompletionSource _productsFetchedTcs = new();
        private readonly UniTaskCompletionSource _purchasesFetchedTcs = new();

        public static event Action<Orders> OnPurchasesFetched;

        public UnityIapBackend(
            List<ProductDefinition> products,
            IIapPurchaseProcessor processor,
            string storeName = null)
        {
            _products = products;
            _processor = processor;
            _store = UnityIAPServices.StoreController(storeName);

            _store.OnPurchasePending += HandlePending;

            // _store.OnPurchasesFetched += orders =>
            // {
            //     foreach (var order in orders.PendingOrders)
            //     {
            //         HandlePending(order);   
            //     }
            // };

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

            _store.OnStoreDisconnected += e =>
            {
                Debug.LogError($"[IAP] Store disconnected: {e.Message}");
            };

            _store.OnProductsFetchFailed += e =>
            {
                foreach (var p in e.FailedFetchProducts)
                    Debug.LogError($"[IAP] Product fetch failed: {p.id}, reason={e.FailureReason}");
            };

            _store.OnProductsFetched += OnProductsFetched;
            _store.OnPurchasesFetched += OnPurchasesFetchedInternal;
        }

        // --------------------------------------------------------------------

        public async UniTask InitializeAsync()
        {
            if (_initialized)
                return;

            _initialized = true;
            
            await _store.Connect().AsUniTask();

            _store.FetchProducts(_products);
            _store.FetchPurchases();

// #if UNITY_IOS && !UNITY_EDITOR
//             VContainer.ADS.NotifySensitiveFlowStarted();
//             _store.RestoreTransactions((ok, err) =>
//             {
//                 VContainer.ADS.NotifySensitiveFlowEnded();
//                 if (!ok)
//                     Debug.LogError($"[IAP] RestoreTransactions failed: {err}");
//             });
// #endif
        }

        // --------------------------------------------------------------------

        public UniTask WaitForProductsFetched() => _productsFetchedTcs.Task;
        public UniTask WaitForPurchasesFetched() => _purchasesFetchedTcs.Task;

        // --------------------------------------------------------------------

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

        // --------------------------------------------------------------------
        // Products (가격/메타 정보)
        // --------------------------------------------------------------------

        private void OnProductsFetched(List<Product> products)
        {
            if (products == null)
                return;

            foreach (var p in products)
            {
                if (p?.definition == null)
                    continue;
                
                if (!string.IsNullOrEmpty(p.metadata?.localizedPriceString))
                    PriceUpdated?.Invoke(p.definition.id, p.metadata.localizedPriceString);
            }

            _productsFetchedTcs.TrySetResult();
        }

        // --------------------------------------------------------------------
        // Purchases (소유 / 복원)
        // --------------------------------------------------------------------

        private void OnPurchasesFetchedInternal(Orders orders)
        {
           
            foreach (var order in orders.ConfirmedOrders)
            {
                var receipt = order.Info.Receipt;
                var tx = order.Info.TransactionID;
                
                if (string.IsNullOrEmpty(receipt))
                    continue;

                foreach (var pi in order.Info.PurchasedProductInfo)
                {
                    Debug.Log($"[IAP] Owned productId={pi.productId}");
                    OwnedNonConsumableFound?.Invoke(pi.productId, receipt);
                }
            }

            _purchasesFetchedTcs.TrySetResult();
            OnPurchasesFetched?.Invoke(orders);
        }

        // --------------------------------------------------------------------
        // Pending Purchase
        // --------------------------------------------------------------------

        private async void HandlePending(PendingOrder order)
        {
            var info = order.Info;
            if (info.PurchasedProductInfo.Count == 0)
                return;

            foreach (var p in info.PurchasedProductInfo)
            {
                var ctx = new IapPurchaseContext
                {
                    StoreProductId = p.productId,
                    TransactionId = info.TransactionID,
                    UnityReceipt = info.Receipt
                };

                var ok = await _processor.ProcessAsync(ctx);

                if (!ok)
                    return;
            }

#if AESFW_ANALYTICS_SINGULAR
            SingularSDK.InAppPurchase(order);
#endif

            _store.ConfirmPurchase(order);
        }

        // --------------------------------------------------------------------
        // Receipt Validation
        // --------------------------------------------------------------------

        public bool ValidateReceipt(
            string receipt,
            string productId,
            byte[] googleTangle,
            byte[] appleTangle)
        {
            try
            {
                Debug.Log($"[IAP] googleTangle null={(googleTangle==null)} len={(googleTangle?.Length ?? -1)}");
                if (googleTangle != null && googleTangle.Length >= 2)
                    Debug.Log($"[IAP] googleTangle head={BitConverter.ToString(googleTangle, 0, 2)}");
                
                var validator = new CrossPlatformValidator(
                    googleTangle,
                    appleTangle,
                    Application.identifier);

                var result = validator.Validate(receipt);

#if UNITY_ANDROID
                GooglePlayReceipt latestGoogleReceipt = null;
                foreach (var purchaseReceipt in result)
                {
                    if (purchaseReceipt is not GooglePlayReceipt googlePlayReceipt
                        || googlePlayReceipt.productID != productId)
                    {
                        continue;
                    }
                    
                    if (latestGoogleReceipt == null || googlePlayReceipt.purchaseDate > latestGoogleReceipt.purchaseDate)
                    {
                        latestGoogleReceipt = googlePlayReceipt;
                    }
                }

                return latestGoogleReceipt is { purchaseState: GooglePurchaseState.Purchased };
#elif UNITY_IOS
                AppleInAppPurchaseReceipt latestAppleReceipt = null;
                foreach (var purchaseReceipt in result)
                {
                    if (purchaseReceipt is not AppleInAppPurchaseReceipt appleReceipt
                        || appleReceipt.productID != productId)
                    {
                        continue;
                    }

                    if (latestAppleReceipt == null || appleReceipt.purchaseDate > latestAppleReceipt.purchaseDate)
                    {
                        latestAppleReceipt = appleReceipt;
                    }
                }
                
                return latestAppleReceipt != null && latestAppleReceipt.cancellationDate == DateTime.MinValue;
#else
                return true;
#endif
            }
            catch (IAPSecurityException e)
            {
                Debug.LogWarning($"[IAP] Receipt validation failed: {e}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAP] Receipt validation exception: {e}");
                return false;
            }
        }
        
        public bool TryGetProduct(string productId, out Product product)
        {
            product = null;
            product = _store.GetProductById(productId);
            
            return product != null;
        }
        
        public bool TryGetReceipt(string productIdOrSku, out string receipt)
        {
            receipt = null;
            if (string.IsNullOrWhiteSpace(productIdOrSku))
                return false;

            // 1) 기본: id로 찾기
            var p = _store.GetProductById(productIdOrSku);

            // 2) 못 찾으면: storeSpecificId로 재탐색
            if (p == null)
            {
                // StoreController에 전체 제품 목록 접근 방법이 있으면 그걸 쓰세요.
                // 예: _store.Products 같은 API가 없다면, _products(정의 목록)로 매핑을 만들어야 합니다.
                foreach (var def in _products)
                {
                    if (def == null) continue;
                    if (!string.Equals(def.storeSpecificId, productIdOrSku, StringComparison.Ordinal)) continue;

                    p = _store.GetProductById(def.id);
                    break;
                }
            }

            if (p == null || !p.hasReceipt || string.IsNullOrWhiteSpace(p.receipt))
            {
                Debug.LogWarning($"[IAP] No receipt (treated as not-owned). key={productIdOrSku}, productNull={p == null}, hasReceipt={(p != null ? p.hasReceipt.ToString() : "N/A")}");
                return false;
            }

            receipt = p.receipt;
            return true;
        }
    }
}
#endif
