using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Purchasing;

namespace AES.IAP
{
    /// <summary>Low-level store backend (Unity IAP). Uses Store ProductId (=SKU).</summary>
    public interface IIapPurchaseBackend
    {
        UniTask PurchaseAsync(string storeProductId);
    }

    /// <summary>
    /// Receives purchases from the store backend and decides whether the store purchase should be confirmed.
    /// Return true => confirmed (consumed). Return false => keep pending (user can retry).
    /// </summary>
    public interface IIapPurchaseProcessor
    {
        UniTask<bool> ProcessAsync(IapPurchaseContext ctx);
    }

    /// <summary>Applies rewards into the game economy. Game must implement this.</summary>
    public interface IIapRewardApplier
    {
        UniTask ApplyAsync(IReadOnlyList<Data.IapBundleContentRow> rewards);
    }

    /// <summary>Optional server-side receipt verification.</summary>
    public interface IReceiptVerifier
    {
        UniTask<VerifyResult> VerifyAsync(IapPurchaseContext ctx);
    }

    public sealed class VerifyResult
    {
        public bool Ok;
        public string Error;

        public static VerifyResult Success() => new VerifyResult { Ok = true };
        public static VerifyResult Fail(string error) => new VerifyResult { Ok = false, Error = error };
    }

    /// <summary>Transaction dedupe store. Prevents double granting across app restarts.</summary>
    public interface IIapTransactionStore
    {
        bool IsProcessed(string transactionId);
        UniTask MarkProcessedAsync(string transactionId);
        UniTask LoadAsync();
    }

    /// <summary>Minimal purchase context emitted by the store backend.</summary>
    public sealed class IapPurchaseContext
    {
        public string StoreProductId; // SKU
        public string TransactionId;
        public string UnityReceipt;
        public ProductType ProductType;
    }

    /// <summary>
    /// Catalog entry used to initialize Unity IAP.
    /// StoreProductId must match platform store SKU.
    /// </summary>
    public sealed class IapStoreCatalogEntry
    {
        public string StoreProductId;
        public ProductType ProductType;
        public bool VerifyOnServer;
    }
}
