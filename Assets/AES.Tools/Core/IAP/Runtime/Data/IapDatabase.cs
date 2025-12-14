using System;
using System.Collections.Generic;
using System.Linq;

namespace AES.IAP.Data
{
    /// <summary>
    /// Pure data index. No Unity IAP logic.
    ///  - ProductKey -> meta
    ///  - (ProductKey, Platform) -> SKU
    ///  - (SKU, Platform) -> ProductKey
    ///  - ProductKey -> rewards
    /// </summary>
    public sealed class IapDatabase
    {
        private readonly Dictionary<string, IapProductRow> _productByKey;
        private readonly Dictionary<(string key, string platform), string> _skuByKeyPlatform;
        private readonly Dictionary<(string sku, string platform), string> _keyBySkuPlatform;
        private readonly Dictionary<string, List<IapBundleContentRow>> _bundleByKey;

        public IapDatabase(
            IEnumerable<IapProductRow> products,
            IEnumerable<IapStoreProductRow> storeProducts,
            IEnumerable<IapBundleContentRow> bundleContents)
        {
            _productByKey = products
                .Where(x => !string.IsNullOrWhiteSpace(x.ProductKey))
                .ToDictionary(x => x.ProductKey, x => x, StringComparer.Ordinal);

            _skuByKeyPlatform = new();
            _keyBySkuPlatform = new();

            foreach (var s in storeProducts)
            {
                if (!s.IsActive) continue;
                if (string.IsNullOrWhiteSpace(s.ProductKey) || string.IsNullOrWhiteSpace(s.Platform) || string.IsNullOrWhiteSpace(s.StoreProductId))
                    continue;

                _skuByKeyPlatform[(s.ProductKey, s.Platform)] = s.StoreProductId.Trim();
                _keyBySkuPlatform[(s.StoreProductId.Trim(), s.Platform)] = s.ProductKey;
            }

            _bundleByKey = bundleContents
                .Where(x => !string.IsNullOrWhiteSpace(x.BundleKey))
                .GroupBy(x => x.BundleKey)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DisplayOrder).ToList(), StringComparer.Ordinal);
        }

        public bool TryResolveSku(string productKey, out string sku)
            => _skuByKeyPlatform.TryGetValue((productKey, IapPlatform.Current), out sku);

        public bool TryResolveProductKeyBySku(string sku, out string productKey)
            => _keyBySkuPlatform.TryGetValue((sku, IapPlatform.Current), out productKey);

        public IReadOnlyList<IapBundleContentRow> GetRewards(string productKey)
            => _bundleByKey.TryGetValue(productKey, out var list) ? list : Array.Empty<IapBundleContentRow>();

        public bool TryGetProduct(string productKey, out IapProductRow row)
            => _productByKey.TryGetValue(productKey, out row);

        public IEnumerable<(string productKey, string sku)> EnumerateActiveSkusForCurrentPlatform()
        {
            var platform = IapPlatform.Current;
            foreach (var kv in _skuByKeyPlatform)
            {
                if (!string.Equals(kv.Key.platform, platform, StringComparison.Ordinal)) continue;
                yield return (kv.Key.key, kv.Value);
            }
        }
    }
}
