#if AESFW_IAP
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
                
                // UnityIapBackend.OwnedNonConsumableFound += (productId, receipt) =>
                // {
                //     if (_db.TryResolveProductKeyByProductId(productId, out var productKey))
                //     {
                //         PurchaseConfirmedByProductKey?.Invoke(productKey);
                //     }
                // };
            }

            Ready?.Invoke();
        }


        private void OnBackendPriceUpdated(string productId, string priceText)
        {
            try
            {
                // Debug.Log($"[IAP] OnBackendPriceUpdated pid={productId}, price={priceText}");

                if (string.IsNullOrWhiteSpace(productId)) return;
                if (string.IsNullOrWhiteSpace(priceText)) return;

                if (_db != null && _db.TryResolveProductKeyByProductId(productId, out var productKey))
                {
                    _productKeyToPrice[productKey] = priceText;

                    // 기존:
                    // PriceUpdatedByProductKey?.Invoke(productKey, priceText);

                    // 변경: 구독자(리스너)별로 분해 호출 + 예외 분리 로깅
                    var evt = PriceUpdatedByProductKey;

                    if (evt != null)
                    {
                        foreach (var d in evt.GetInvocationList())
                        {
                            var cb = (Action<string, string>)d;

                            try { cb(productKey, priceText); }
                            catch (Exception ex)
                            {
                                Debug.LogError(
                                    $"[IAP] PriceUpdatedByProductKey handler FAILED " +
                                    $"target={cb.Target?.GetType().FullName ?? "static"} " +
                                    $"method={cb.Method.DeclaringType?.FullName}.{cb.Method.Name} " +
                                    $"productKey={productKey} price={priceText}\n{ex}"
                                );
                            }
                        }
                    }

                    //Debug.Log($"[IAP] Resolved productKey={productKey}");
                }
                else
                {
                    Debug.LogError($"[IAP] Resolve FAILED productId={productId}");
                }
            }
            catch (Exception e) { Debug.LogError($"[IAP] OnBackendPriceUpdated FAILED pid={productId}, price={priceText}\n{e}"); }
        }


        private void OnBackendConfirmed(Order order)
        {
            try
            {
                if (_db == null || order == null) return;

                var purchased = order.Info.PurchasedProductInfo;
                if (purchased.Count == 0) return;

                foreach (var p in purchased)
                {
                    var productId = p?.productId;
                    if (string.IsNullOrWhiteSpace(productId)) continue;

                    if (_db.TryResolveProductKeyByProductId(productId, out var productKey) &&
                        !string.IsNullOrWhiteSpace(productKey))
                    {
                        var evt = PurchaseConfirmedByProductKey;

                        if (evt != null)
                        {
                            foreach (var d in evt.GetInvocationList())
                            {
                                var cb = (Action<string>)d;

                                try { cb(productKey); }
                                catch (Exception ex)
                                {
                                    Debug.LogError(
                                        $"[IAP] PurchaseConfirmedByProductKey handler FAILED " +
                                        $"target={cb.Target?.GetType().FullName ?? "static"} " +
                                        $"method={cb.Method.DeclaringType?.FullName}.{cb.Method.Name} " +
                                        $"productKey={productKey}\n{ex}"
                                    );
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) { Debug.LogError($"[IAP] OnBackendConfirmed FAILED\n{e}"); }
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
        
        public UniTask  WaitForProductsFetched() => _backend.WaitForProductsFetched();

        public bool ValidateReceiptByProductKey(string productKey, string receipt, byte[] googleTangle, byte[] appleTangle)
        {
            if (!IsReady) return false;

            if (string.IsNullOrWhiteSpace(productKey))
            {
                Debug.LogError("[IAP] ValidateReceiptByProductKey: productKey is empty.");
                return false;
            }

            if (!_db.TryResolveProductId(productKey, out var productId) || string.IsNullOrWhiteSpace(productId))
            {
                Debug.LogError($"[IAP] ValidateReceiptByProductKey: ProductId not found for productKey: {productKey}");
                return false;
            }


            if (_backend is not UnityIapBackend unityBackend)
            {
                Debug.LogError("[IAP] ValidateReceiptByProductKey: Backend is not UnityIapBackend.");
                return false;
            }

            //
            // if (unityBackend.TryGetProduct(productId, out var product) && product != null)
            // {
            //     // Consumable은 영수증 검증 대상에서 제외 (원본 코드와 동일)
            //     if (product.definition.type == ProductType.Consumable)
            //     {
            //         Debug.Log("[IAP] ValidateReceiptByProductKey: Consumable product cannot be validated.");
            //         return false;
            //     }
            //
            //
            //     // Non-Consumable / Subscription인데 영수증이 없으면 실패
            //     if (!product.hasReceipt || string.IsNullOrWhiteSpace(product.receipt))
            //     {
            //         Debug.LogError("[IAP] ValidateReceiptByProductKey: No receipt found for non-consumable/subscription product.");
            //         return false;
            //     }
            //
            //     var result = unityBackend.ValidateReceipt(product.receipt, productId, googleTangle, appleTangle);
            //     Debug.Log($"[IAP] ValidateReceiptByProductKey: result={result}");
            //     return result;
            // }
            //
            // if (!unityBackend.TryGetReceipt(productId, out var receipt) || string.IsNullOrWhiteSpace(receipt))
            // {
            //     Debug.LogError("[IAP] ValidateReceiptByProductKey: Failed to get receipt.");
            //     return false;
            // }

            var validate = unityBackend.ValidateReceipt(receipt, productId, googleTangle, appleTangle);
            Debug.Log($"[IAP] ValidateReceiptByProductKey: receipt={receipt}");
            Debug.Log($"[IAP] ValidateReceiptByProductKey: result={validate}");
            return validate;
        }
    }
}
#endif