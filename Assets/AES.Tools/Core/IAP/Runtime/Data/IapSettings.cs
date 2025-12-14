using UnityEngine;

namespace Systems.IAP
{
    [CreateAssetMenu(menuName = "IAP/Iap Settings", fileName = "IapSettings")]
    public sealed class IapSettings : ScriptableObject
    {
        [Header("Resources paths (no extension)")]
        public string productsJsonPath = "IAP/Generated/IapProduct";
        public string storeProductsJsonPath = "IAP/Generated/IapStoreProduct";
        public string bundleContentsJsonPath = "IAP/Generated/IapBundleContent";

        [Header("Behavior")]
        public bool enablePlayerPrefsTxFallback = true;
    }
}