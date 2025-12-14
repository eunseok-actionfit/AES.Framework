using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using AES.IAP.Data;
using AES.IAP.Flow;
using AES.IAP.Transaction;
using AES.IAP.Unity;

namespace AES.IAP
{
    /// <summary>
    /// Convenience wiring without DI.
    /// If you use DI (VContainer/Zenject), register these pieces instead.
    /// </summary>
    public static class IapBootstrap
    {
        public sealed class Result
        {
            public IapDatabase Db;
            public UnityIapService Store;
            public IapPurchaseFlow Flow;
            public IReadOnlyList<IapStoreCatalogEntry> Catalog;
        }

        /// <summary>
        /// Creates a ready-to-use IAP pipeline.
        /// You must provide your game reward applier (economy).
        /// </summary>
        public static async UniTask<Result> CreateAsync(
            string productsJsonPath,
            string storeProductsJsonPath,
            string bundleContentsJsonPath,
            IIapRewardApplier rewardApplier,
            IIapTransactionStore txStore = null,
            IReceiptVerifier verifier = null)
        {
            var db = IapDatabaseLoader_Resources.Load(productsJsonPath, storeProductsJsonPath, bundleContentsJsonPath);
            var catalog = IapUnityCatalogBuilder.Build(db);

            txStore ??= new TransactionStore_PlayerPrefs();

            // Flow is also the processor
            UnityIapService store = null;
            IapPurchaseFlow flow = null;

            // Create store first with placeholder processor, then create flow using store as backend.
            // To avoid circular dependency in pure C#, we create flow after store and then set processor via adapter.
            var processorAdapter = new ProcessorAdapter();
            store = new UnityIapService(catalog, processorAdapter, txStore, verifier);
            flow = new IapPurchaseFlow(db, store, rewardApplier);
            processorAdapter.Target = flow;

            await store.InitializeAsync();

            return new Result
            {
                Db = db,
                Store = store,
                Flow = flow,
                Catalog = catalog,
            };
        }

        private sealed class ProcessorAdapter : IIapPurchaseProcessor
        {
            public IIapPurchaseProcessor Target;
            public UniTask<bool> ProcessAsync(IapPurchaseContext ctx) => Target.ProcessAsync(ctx);
        }
    }
}
