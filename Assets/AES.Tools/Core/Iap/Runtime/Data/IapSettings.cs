using UnityEngine;


namespace AES.Tools
{
    [CreateAssetMenu(menuName = "IAP/Iap Settings", fileName = "IapSettings")]
    public sealed class IapSettings : ScriptableObject
    {
        [Header("Resources folder (preferred)")]
        [Tooltip("Folder under Resources. Example: IAP/Generated")]
        public string generatedFolder = "IAP/Generated";

        [Header("Legacy resources paths (no extension) - fallback when generatedFolder is empty")]
        public string productsJsonPath = "IAP/Generated/IapProduct";
        public string storeProductsJsonPath = "IAP/Generated/IapStoreProduct";
        public string bundleContentsJsonPath = "IAP/Generated/IapBundleContent";

        [Header("Behavior")]
        public bool enablePlayerPrefsTxFallback = true;
    }
}