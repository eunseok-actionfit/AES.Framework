using System;
using UnityEngine;

namespace AES.IAP.Data
{
    public static class IapDatabaseLoader_Resources
    {
        public static IapDatabase Load(string iapProductPath, string iapStoreProductPath, string iapBundleContentPath)
        {
            var prodText = Resources.Load<TextAsset>(iapProductPath);
            var storeText = Resources.Load<TextAsset>(iapStoreProductPath);
            var bundleText = Resources.Load<TextAsset>(iapBundleContentPath);

            if (prodText == null) throw new Exception($"Missing TextAsset: {iapProductPath}");
            if (storeText == null) throw new Exception($"Missing TextAsset: {iapStoreProductPath}");
            if (bundleText == null) throw new Exception($"Missing TextAsset: {iapBundleContentPath}");

            var prods = JsonUtility.FromJson<JsonRoot<IapProductRow>>(prodText.text)?.rows ?? new();
            var stores = JsonUtility.FromJson<JsonRoot<IapStoreProductRow>>(storeText.text)?.rows ?? new();
            var bundles = JsonUtility.FromJson<JsonRoot<IapBundleContentRow>>(bundleText.text)?.rows ?? new();

            return new IapDatabase(prods, stores, bundles);
        }
    }
}
