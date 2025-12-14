using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using Unity.Services.Core;

namespace AES.IAP.Unity
{
    /// <summary>
    /// Unity IAP wrapper.
    /// Responsibilities:
    ///  - Initialize store with catalog entries (SKUs)
    ///  - Start purchase (by SKU)
    ///  - Emit purchases to IIapPurchaseProcessor
    ///  - Confirm pending purchase ONLY when processor returns true
    /// </summary>
    public sealed class UnityIapService : IIapPurchaseBackend, IStoreListener
    {
        private readonly IReadOnlyList<IapStoreCatalogEntry> _catalog;
        private readonly IIapPurchaseProcessor _processor;
        private readonly IIapTransactionStore _txStore; // optional
        private readonly IReceiptVerifier _verifier;    // optional

        private IStoreController _store;
        private IExtensionProvider _extensions;

        private UniTaskCompletionSource _initTcs;
        private bool _initializing;

        public bool IsInitialized => _store != null;

        public UnityIapService(
            IReadOnlyList<IapStoreCatalogEntry> catalog,
            IIapPurchaseProcessor processor,
            IIapTransactionStore txStore = null,
            IReceiptVerifier verifier = null)
        {
            _catalog = catalog;
            _processor = processor;
            _txStore = txStore;
            _verifier = verifier;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (_catalog == null || _catalog.Count == 0)
                throw new InvalidOperationException("[IAP] Catalog is empty (no SKUs).");
            if (_processor == null)
                throw new InvalidOperationException("[IAP] IIapPurchaseProcessor is not registered.");

            if (_initializing)
            {
                await _initTcs.Task;
                return;
            }

            _initializing = true;
            _initTcs = new UniTaskCompletionSource();

            try
            {
                if (_txStore != null)
                    await _txStore.LoadAsync();

                await UnityServices.InitializeAsync();

                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                foreach (var e in _catalog)
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.StoreProductId))
                        throw new InvalidOperationException("[IAP] Catalog contains empty StoreProductId.");

                    builder.AddProduct(e.StoreProductId, e.ProductType);
                }

                UnityPurchasing.Initialize(this, builder);
                await _initTcs.Task;
            }
            finally
            {
                _initializing = false;
            }
        }

        public UniTask PurchaseAsync(string storeProductId)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("[IAP] Not initialized");

            var product = _store.products.WithID(storeProductId);
            if (product == null || !product.availableToPurchase)
                throw new InvalidOperationException($"[IAP] Product not available: {storeProductId}");

            _store.InitiatePurchase(product);
            return UniTask.CompletedTask;
        }

        public UniTask RestoreAsync()
        {
#if UNITY_IOS
            if (!IsInitialized) return UniTask.CompletedTask;
            var apple = _extensions.GetExtension<IAppleExtensions>();
            var tcs = new UniTaskCompletionSource();
            apple.RestoreTransactions(_ => tcs.TrySetResult());
            return tcs.Task;
#else
            return UniTask.CompletedTask;
#endif
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _store = controller;
            _extensions = extensions;
            _initTcs?.TrySetResult();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
            => _initTcs?.TrySetException(new Exception($"[IAP] Init failed: {error}"));

        public void OnInitializeFailed(InitializationFailureReason error, string message)
            => _initTcs?.TrySetException(new Exception($"[IAP] Init failed: {error} / {message}"));

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            HandlePurchaseAsync(e).Forget();
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            Debug.LogWarning($"[IAP] Purchase failed: {product?.definition?.id} / {reason}");
        }

        private async UniTaskVoid HandlePurchaseAsync(PurchaseEventArgs e)
        {
            var sku = e.purchasedProduct.definition.id;
            var txId = e.purchasedProduct.transactionID;

            // already processed => confirm immediately
            if (!string.IsNullOrEmpty(txId) && _txStore != null && _txStore.IsProcessed(txId))
            {
                _store.ConfirmPendingPurchase(e.purchasedProduct);
                return;
            }

            var ctx = new IapPurchaseContext
            {
                StoreProductId = sku,
                TransactionId = txId,
                UnityReceipt = e.purchasedProduct.receipt,
                ProductType = e.purchasedProduct.definition.type,
            };

            // Optional verify (per product flag)
            var verifyRequired = false;
            for (int i = 0; i < _catalog.Count; i++)
            {
                var c = _catalog[i];
                if (c != null && string.Equals(c.StoreProductId, sku, StringComparison.Ordinal))
                {
                    verifyRequired = c.VerifyOnServer;
                    break;
                }
            }

            if (verifyRequired && _verifier != null)
            {
                VerifyResult vr;
                try { vr = await _verifier.VerifyAsync(ctx); }
                catch (Exception ex)
                {
                    Debug.LogError($"[IAP] Verify exception: {ex}");
                    return; // keep pending
                }

                if (vr == null || !vr.Ok)
                {
                    Debug.LogWarning($"[IAP] Verify failed: {vr?.Error}");
                    return; // keep pending
                }
            }

            bool ok;
            try { ok = await _processor.ProcessAsync(ctx); }
            catch (Exception ex)
            {
                Debug.LogError($"[IAP] Processor exception: {ex}");
                return; // keep pending
            }

            if (!ok) return; // keep pending

            if (!string.IsNullOrEmpty(txId) && _txStore != null)
                await _txStore.MarkProcessedAsync(txId);

            _store.ConfirmPendingPurchase(e.purchasedProduct);
        }
    }
}
