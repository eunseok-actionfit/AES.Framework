#if AESFW_IAP
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing;


namespace AES.Tools
{
    public static class IapUnityCatalogBuilder
    {
        public static List<IapStoreCatalogEntry> Build(IapDatabase db)
        {
            var list = new List<IapStoreCatalogEntry>();

            foreach (var (productKey, productId) in db.EnumerateActiveProductIdsForCurrentPlatform())
            {
                if (string.IsNullOrWhiteSpace(productId)) continue;
                if (!db.TryGetProduct(productKey, out var meta)) continue;
                if (!meta.IsVisible) continue;

                list.Add(new IapStoreCatalogEntry
                {
                    // Unity IAP v5 uses ProductDefinition.id as the canonical identifier.
                    StoreProductId = productId,
                    ProductType = ParseProductType(meta.ProductType),
                    VerifyOnServer = meta.VerifyOnServer,
                });
            }

            // stable
            list = list
                .GroupBy(x => x.StoreProductId, StringComparer.Ordinal)
                .Select(g => g.First())
                .OrderBy(x => x.StoreProductId, StringComparer.Ordinal)
                .ToList();

            return list;
        }

        private static ProductType ParseProductType(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return ProductType.Consumable;
            if (Enum.TryParse<ProductType>(s, true, out var pt)) return pt;

            var norm = s.Replace("-", "").Replace("_", "").Replace(" ", "");
            if (string.Equals(norm, "NonConsumable", StringComparison.OrdinalIgnoreCase)) return ProductType.NonConsumable;
            if (string.Equals(norm, "Subscription", StringComparison.OrdinalIgnoreCase)) return ProductType.Subscription;
            return ProductType.Consumable;
        }
    }
}
#endif