using System;
using System.Collections.Generic;

namespace AES.IAP.Data
{
    [Serializable]
    public sealed class JsonRoot<T>
    {
        public List<T> rows = new();
    }

    [Serializable]
    public sealed class IapProductRow
    {
        public string ProductKey;
        public string ProductType;   // Consumable/NonConsumable/Subscription
        public string Category;
        public string DisplayGroup;
        public bool IsVisible;
        public int SortOrder;
        public string AnalyticsId;
        public string Note;
        public bool VerifyOnServer;
    }

    [Serializable]
    public sealed class IapStoreProductRow
    {
        public string ProductKey;
        public string Platform;        // GP/IOS
        public string StoreProductId;  // SKU
        public int Price;
        public string Currency;
        public bool IsActive;
    }

    [Serializable]
    public sealed class IapBundleContentRow
    {
        public string BundleKey;   // = ProductKey
        public string ItemType;    // Coin/Booster/RemoveAds/UnlimitedLivesMin
        public string ItemId;      // Booster id
        public int Amount;
        public bool IsMain;
        public int DisplayOrder;
    }
}
