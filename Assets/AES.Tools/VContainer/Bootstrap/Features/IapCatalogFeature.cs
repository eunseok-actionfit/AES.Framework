using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    [CreateAssetMenu(menuName="Bootstrap/Features/IAP Catalog", fileName="IapCatalogFeature")]
    public sealed class IapCatalogFeature : AppFeatureSO, IProvideCapabilities, IIapCatalogProvider
    {
        [SerializeField] private List<AES.IAP.IapStoreCatalogEntry> entries = new();

        public IReadOnlyList<AES.IAP.IapStoreCatalogEntry> GetCatalog() => entries;

        public void Provide(IDictionary<Type, IFeatureCapability> caps)
        {
            caps[typeof(IIapCatalogProvider)] = this;
        }

        public override void Install(IContainerBuilder builder, in FeatureContext ctx)
        {
            builder.RegisterInstance((IReadOnlyList<AES.IAP.IapStoreCatalogEntry>)entries);
        }
    }
}