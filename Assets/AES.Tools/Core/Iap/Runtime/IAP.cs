using System;
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;

namespace AES.Tools
{
    public static class IAP
    {
        private static IIap _service;

        internal static void Bind(IIap service) => _service = service;

        public static bool IsBound => _service != null;
        public static bool IsReady => _service?.IsReady ?? false;
        
        public static IapDatabase Database => _service?.Database;
        
        public static event Action Ready
        {
            add { if (_service != null) _service.Ready += value; }
            remove { if (_service != null) _service.Ready -= value; }
        }

        public static event Action<string, string> PriceUpdatedByProductKey
        {
            add { if (_service != null) _service.PriceUpdatedByProductKey += value; }
            remove { if (_service != null) _service.PriceUpdatedByProductKey -= value; }
        }
        
        public static event Action<string> PurchaseConfirmedByProductKey
        {
            add { if (_service != null) _service.PurchaseConfirmedByProductKey += value; }
            remove { if (_service != null) _service.PurchaseConfirmedByProductKey -= value; }
        }

        public static bool TryGetLocalizedPriceByProductKey(string productKey, out string priceText)
        {
            priceText = null;
            return _service != null && _service.TryGetLocalizedPriceByProductKey(productKey, out priceText);
        }

        public static UniTask PurchaseByProductKeyAsync(string productKey)
        {
            if (_service == null)
                throw new InvalidOperationException("[IAP] Not bound.");
            ADS.NotifySensitiveFlowStarted();
            return _service.PurchaseByProductKeyAsync(productKey);
        }

        public static UniTask RestoreAsync()
            => _service?.RestoreAsync() ?? UniTask.CompletedTask;
    }
}