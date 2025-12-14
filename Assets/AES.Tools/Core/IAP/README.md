# IAP_Slim

Minimal Unity IAP pipeline:

- Buy by **ProductKey** (your internal key)
- Store purchase callback uses **SKU**
- SKU -> ProductKey -> rewards (from DB) -> apply (game code)

## Runtime pieces

- `AES.IAP.Data.IapDatabase` loads and indexes JSON tables.
- `AES.IAP.Unity.UnityIapService` wraps Unity IAP and forwards purchases to a processor.
- `AES.IAP.Flow.IapPurchaseFlow` maps SKU -> rewards and calls your `IIapRewardApplier`.

## Quick start (no DI)

```csharp
// Resources paths (without .json extension)
const string Products = "IAP/Generated/IapProduct";
const string StoreProducts = "IAP/Generated/IapStoreProduct";
const string Bundles = "IAP/Generated/IapBundleContent";

// your implementation
IIapRewardApplier rewardApplier = new MyRewardApplier();

var iap = await AES.IAP.IapBootstrap.CreateAsync(
    Products,
    StoreProducts,
    Bundles,
    rewardApplier);

// UI buys by ProductKey
await iap.Flow.PurchaseByProductKeyAsync("starter_pack");
```

## JSON format

Each file is:

```json
{ "rows": [ { ... }, { ... } ] }
```

Tables:

- `IapProductRow`: ProductKey, ProductType, IsVisible, VerifyOnServer...
- `IapStoreProductRow`: ProductKey, Platform, StoreProductId (SKU), IsActive...
- `IapBundleContentRow`: BundleKey (=ProductKey), ItemType, ItemId, Amount...

## Editor (optional)

The `Editor/Sheets` scripts can download Google Sheets (service account) and bake JSON files.
