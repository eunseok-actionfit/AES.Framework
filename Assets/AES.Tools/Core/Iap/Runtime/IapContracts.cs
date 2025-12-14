using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace AES.Tools
{
    /// <summary>Low-level store backend (Unity IAP). Uses Store ProductId (=SKU).</summary>
    public interface IIapPurchaseBackend
    {
        UniTask InitializeAsync();
        UniTask PurchaseAsync(string sku);
        UniTask RestoreAsync();
    }

    /// <summary>
    /// Receives purchases from the store backend and decides whether the store purchase should be confirmed.
    /// Return true => confirm. Return false => keep pending.
    /// </summary>
    public interface IIapPurchaseProcessor
    {
        UniTask<bool> ProcessAsync(IapPurchaseContext ctx);
    }

    /// <summary>Applies rewards into the game economy. Game must implement this.</summary>
    public interface IIapRewardApplier
    {
        UniTask ApplyAsync(IReadOnlyList<IapBundleContentRow> rewards);
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
        // ProductType은 v5 주문모델에서 안정적으로 못 구해서 제거/미사용 권장
    }

    /// <summary>Facade interface exposed to game UI.</summary>
    public interface IIap
    {
        bool IsReady { get; }
        UniTask PurchaseByProductKeyAsync(string productKey);
        UniTask RestoreAsync();
    }

    /// <summary>
    /// Catalog entry used to initialize Unity IAP.
    /// StoreProductId must match platform store SKU.
    /// </summary>
    public sealed class IapStoreCatalogEntry
    {
        public string StoreProductId;
        public UnityEngine.Purchasing.ProductType ProductType;
        public bool VerifyOnServer;
    }
}
