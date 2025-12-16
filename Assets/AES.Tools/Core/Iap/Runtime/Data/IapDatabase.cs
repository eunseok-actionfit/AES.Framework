using System;
using System.Collections.Generic;
using System.Linq;


namespace AES.Tools
{
    public sealed class IapDatabase
    {
        private readonly Dictionary<string, IapProductRow> _productByKey;
        // Unity IAP v5 canonical id (ProductDefinition.id) mapping.
        // productKey <-> productId (per platform)
        private readonly Dictionary<(string key, string platform), string> _productIdByKeyPlatform;
        private readonly Dictionary<(string productId, string platform), string> _keyByProductIdPlatform;
        private readonly Dictionary<string, List<IapBundleContentRow>> _bundleByKey;

        // 추가 인덱스
        private readonly Dictionary<string, List<EnumDefinitionRow>> _enumByName;
        private readonly Dictionary<(string type, string id), double> _valueInGemByItem;
        private readonly Dictionary<string, List<IapLimitRow>> _limitsByProductKey;

        // 기존 생성자(레거시 유지)

        // 신규 생성자(확장 데이터 포함)
        public IapDatabase(
            IEnumerable<IapProductRow> products,
            IEnumerable<IapStoreProductRow> storeProducts,
            IEnumerable<IapBundleContentRow> bundleContents,
            IEnumerable<EnumDefinitionRow> enumDefs = null,
            IEnumerable<EconomyValueRow> economyValues = null,
            IEnumerable<IapLimitRow> limits = null)
        {
            products ??= Array.Empty<IapProductRow>();
            storeProducts ??= Array.Empty<IapStoreProductRow>();
            bundleContents ??= Array.Empty<IapBundleContentRow>();

            _productByKey = products
                .Where(x => !string.IsNullOrWhiteSpace(x.ProductKey))
                .ToDictionary(x => x.ProductKey.Trim(), x => x, StringComparer.Ordinal);

            _productIdByKeyPlatform = new();
            _keyByProductIdPlatform = new();

            foreach (var s in storeProducts)
            {
                if (!s.IsActive) continue;
                if (string.IsNullOrWhiteSpace(s.ProductKey) || string.IsNullOrWhiteSpace(s.Platform) || string.IsNullOrWhiteSpace(s.StoreProductId))
                    continue;

                var key = s.ProductKey.Trim();
                var platform = s.Platform.Trim();
                // NOTE: This is treated as Unity IAP productId (ProductDefinition.id).
                // If your data schema differentiates productId vs storeSpecificId, map the correct column here.
                var productId = s.StoreProductId.Trim();

                _productIdByKeyPlatform[(key, platform)] = productId;
                _keyByProductIdPlatform[(productId, platform)] = key;
            }

            _bundleByKey = bundleContents
                .Where(x => !string.IsNullOrWhiteSpace(x.BundleKey))
                .GroupBy(x => x.BundleKey.Trim())
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DisplayOrder).ToList(), StringComparer.Ordinal);

            // ---- EnumDefinition index ----
            _enumByName = (enumDefs ?? Array.Empty<EnumDefinitionRow>())
                .Where(x => !string.IsNullOrWhiteSpace(x.EnumName) && x.IsActive)
                .GroupBy(x => x.EnumName.Trim(), StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.SortOrder).ToList(),
                    StringComparer.Ordinal);

            // ---- Economy value index ----
            _valueInGemByItem = new Dictionary<(string, string), double>();
            foreach (var e in (economyValues ?? Array.Empty<EconomyValueRow>()))
            {
                if (string.IsNullOrWhiteSpace(e.ItemType) || string.IsNullOrWhiteSpace(e.ItemId)) continue;
                _valueInGemByItem[(e.ItemType.Trim(), e.ItemId.Trim())] = e.ValueInGem;
            }

            // ---- Limits index ----
            _limitsByProductKey = (limits ?? Array.Empty<IapLimitRow>())
                .Where(x => !string.IsNullOrWhiteSpace(x.ProductKey))
                .GroupBy(x => x.ProductKey.Trim(), StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        }

        public bool TryResolveProductId(string productKey, out string productId)
            => _productIdByKeyPlatform.TryGetValue((productKey, IapPlatform.Current), out productId);

        public bool TryResolveProductKeyByProductId(string productId, out string productKey)
            => _keyByProductIdPlatform.TryGetValue((productId, IapPlatform.Current), out productKey);

        [Obsolete("Use TryResolveProductId instead.")]
        public bool TryResolveSku(string productKey, out string sku)
            => TryResolveProductId(productKey, out sku);

        [Obsolete("Use TryResolveProductKeyByProductId instead.")]
        public bool TryResolveProductKeyBySku(string sku, out string productKey)
            => TryResolveProductKeyByProductId(sku, out productKey);

        public IReadOnlyList<IapBundleContentRow> GetRewards(string productKey)
            => _bundleByKey.TryGetValue(productKey, out var list) ? list : Array.Empty<IapBundleContentRow>();

        public bool TryGetProduct(string productKey, out IapProductRow row)
            => _productByKey.TryGetValue(productKey, out row);

        public IReadOnlyList<EnumDefinitionRow> GetEnum(string enumName)
            => _enumByName.TryGetValue(enumName, out var list) ? list : Array.Empty<EnumDefinitionRow>();

        public bool TryGetValueInGem(string itemType, string itemId, out double value)
            => _valueInGemByItem.TryGetValue((itemType, itemId), out value);

        public IReadOnlyList<IapLimitRow> GetLimits(string productKey)
            => _limitsByProductKey.TryGetValue(productKey, out var list) ? list : Array.Empty<IapLimitRow>();

        public IEnumerable<(string productKey, string productId)> EnumerateActiveProductIdsForCurrentPlatform()
        {
            var platform = IapPlatform.Current;
            foreach (var kv in _productIdByKeyPlatform)
            {
                if (!string.Equals(kv.Key.platform, platform, StringComparison.Ordinal)) continue;
                yield return (kv.Key.key, kv.Value);
            }
        }

        [Obsolete("Use EnumerateActiveProductIdsForCurrentPlatform instead.")]
        public IEnumerable<(string productKey, string sku)> EnumerateActiveSkusForCurrentPlatform()
        {
            foreach (var (productKey, productId) in EnumerateActiveProductIdsForCurrentPlatform())
                yield return (productKey, productId);
        }
    }
}
