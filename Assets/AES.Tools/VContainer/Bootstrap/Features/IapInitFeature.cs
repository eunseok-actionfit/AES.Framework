using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using AES.IAP;
using AES.IAP.Unity;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    public sealed class IapInitFeature : AppFeatureSO
    {
        public override UniTask Initialize(LifetimeScope rootScope,  FeatureContext ctx)
            => InitializeImpl(rootScope,  ctx);

        private async static UniTask InitializeImpl(LifetimeScope rootScope,  FeatureContext ctx)
        {
            // “통합 없으면 조용히 스킵” 정책 유지: catalog가 비면 종료
            if (!ctx.Capabilities.TryGetCapability<IIapCatalogProvider>(out var p))
            {
                Debug.Log("[IAP] Disabled (no catalog capability).");
                return;
            }

            var catalog = p.GetCatalog();
            if (catalog == null || catalog.Count == 0)
            {
                Debug.Log("[IAP] Disabled (catalog empty).");
                return;
            }

           
            var tx = rootScope.Container.Resolve<IIapTransactionStore>();
            var store = rootScope.Container.Resolve<UnityIapService>();

            await tx.LoadAsync();
            await store.InitializeAsync();

            Debug.Log("[IAP] Ready");
        }
    }
}