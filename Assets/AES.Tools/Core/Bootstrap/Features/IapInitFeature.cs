#if AESFW_IAP
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using VContainer;
using VContainer.Unity;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    [CreateAssetMenu(menuName = "Game/Bootstrap/Features/IAP Init Feature", fileName = "IapInitFeature")]
    public sealed class IapInitFeature : AppFeatureSO
    {
        [SerializeField] private string generatedFolder = "IAP/Generated";
        [SerializeField] private bool enablePlayerPrefsTx = true;

        public override void Install(IContainerBuilder builder, in FeatureContext ctx)
        {
            builder.Register<IapFacade>(Lifetime.Singleton).As<IIap>();
            
            try
            {
                var db = IapDatabaseLoader_Resources.Load(generatedFolder);
                builder.RegisterInstance(db).As<IapDatabase>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[IAP] DB not registered (load failed): {e.Message}");
            }
        }

        public override async UniTask Initialize(LifetimeScope rootScope, FeatureContext ctx)
        {
            if (!ctx.Capabilities.TryGetCapability<IIapRewardApplierCapability>(out var cap) || cap?.RewardApplier == null)
            {
                Debug.Log("[IAP] Disabled (no reward applier capability).");
                return;
            }

            var facade = rootScope.Container.Resolve<IIap>() as IapFacade;
            if (facade == null) return;

            IapDatabase db;
            try
            {
                db = IapDatabaseLoader_Resources.Load(generatedFolder);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[IAP] Disabled (load failed): {e.Message}");
                return;
            }

            var entries = IapUnityCatalogBuilder.Build(db);
            if (entries == null || entries.Count == 0)
            {
                Debug.Log("[IAP] Disabled (catalog empty).");
                return;
            }

            var products = entries
                .Select(e => new ProductDefinition(e.StoreProductId, e.ProductType))
                .ToList();

            IIapTransactionStore tx = enablePlayerPrefsTx ? new TransactionStore_PlayerPrefs() : null;
            if (tx != null) await tx.LoadAsync();

            var router = new PurchaseProcessorRouter();

            var processor = new IapPurchaseProcessor(db, cap.RewardApplier, tx, verifier: null);
            router.Target = processor;

            var backend = new UnityIapBackend(products, router);
            facade.SetReady(db, backend);
            IAP.Bind(facade);
            
            await backend.InitializeAsync();
            
            Debug.Log("[IAP] Initialized.");
        }
    }
}
#endif