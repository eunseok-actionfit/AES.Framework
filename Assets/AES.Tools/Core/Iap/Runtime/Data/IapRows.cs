using System;
using System.Collections.Generic;


namespace AES.Tools
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
        public bool VerifyOnServer;
        public bool IsVisible;
        public int SortOrder;
        public string DisplayKey;
        public string AnalyticsId;
        public string Note;

    }

    [Serializable]
    public sealed class IapStoreProductRow
    {
        public string ProductKey;
        public string Platform;        // GP/IOS
        //public string StoreProductId;  // SKU
        public int Price;
        public string Currency;
        public bool IsActive;
    }

    [Serializable]
    public sealed class IapBundleContentRow
    {
        public string BundleKey;   // = ProductKey
        public string ItemType;    // Coin/Boosters/RemoveAds/UnlimitedLivesMin
        public int Amount;
        public bool IsMain;
        public int DisplayOrder;
    }

    // ---- 추가: EnumDefinition ----
    [Serializable]
    public sealed class EnumDefinitionRow
    {
        public string EnumName;
        public string EnumValue;
        public string DisplayName;
        public string Description;
        public int SortOrder;
        public bool IsActive;
    }

    // ---- 추가: Economy_Value ----
    [Serializable]
    public sealed class EconomyValueRow
    {
        public string ItemType;
        public string ItemId;
        public double ValueInGem;
    }

    // ---- 추가: IAP_Limit ----
    [Serializable]
    public sealed class IapLimitRow
    {
        public string ProductKey;
        public string LimitType;
        public int LimitValue;
        public string ResetType;
        public string Note;
    }
}
