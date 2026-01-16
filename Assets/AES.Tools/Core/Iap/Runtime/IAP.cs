using System;
using AES.Tools.VContainer;
using Cysharp.Threading.Tasks;

namespace AES.Tools
{
    public static class IAP
    {
        private static IIap _service;
        
        private static Action _readyHandlers;
        private static Action<string, string> _priceHandlers;
        private static Action<string> _purchaseConfirmedHandlers;
        
        public static IIap Service => _service;

        internal static void Bind(IIap service)
        {
            // 기존 서비스에 붙어있던 핸들러 제거(재바인딩/테스트 환경 대비)
            if (_service != null)
            {
                if (_readyHandlers != null) _service.Ready -= _readyHandlers;
                if (_priceHandlers != null) _service.PriceUpdatedByProductKey -= _priceHandlers;
                if (_purchaseConfirmedHandlers != null) _service.PurchaseConfirmedByProductKey -= _purchaseConfirmedHandlers;
            }

            _service = service;

            // 새 서비스에 누적된 핸들러 부착
            if (_service != null)
            {
                if (_readyHandlers != null) _service.Ready += _readyHandlers;
                if (_priceHandlers != null) _service.PriceUpdatedByProductKey += _priceHandlers;
                if (_purchaseConfirmedHandlers != null) _service.PurchaseConfirmedByProductKey += _purchaseConfirmedHandlers;
            }
        }

        public static bool IsBound => _service != null;
        public static bool IsReady => _service?.IsReady ?? false;
        
        public static IapDatabase Database => _service?.Database;
        
        public static event Action Ready
        {
            add
            {
                _readyHandlers += value;
                if (_service != null) _service.Ready += value;
            }
            remove
            {
                _readyHandlers -= value;
                if (_service != null) _service.Ready -= value;
            }
        }

        public static event Action<string, string> PriceUpdatedByProductKey
        {
            add
            {
                _priceHandlers += value;
                if (_service != null) _service.PriceUpdatedByProductKey += value;
            }
            remove
            {
                _priceHandlers -= value;
                if (_service != null) _service.PriceUpdatedByProductKey -= value;
            }
        }

        public static event Action<string> PurchaseConfirmedByProductKey
        {
            add
            {
                _purchaseConfirmedHandlers += value;
                if (_service != null) _service.PurchaseConfirmedByProductKey += value;
            }
            remove
            {
                _purchaseConfirmedHandlers -= value;
                if (_service != null) _service.PurchaseConfirmedByProductKey -= value;
            }
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
        
        public static bool ValidateReceiptByProductKey(string productKey, string receipt, byte[] googleTangle, byte[] appleTangle)
        {
            if (_service == null)
            {
                return false;
            }

            // 실제 구현은 IapFacade에만 있으므로 캐스팅
            if (_service is IapFacade facade)
                return facade.ValidateReceiptByProductKey(productKey, receipt, googleTangle, appleTangle);

            return false;
        }
        
        public static UniTask WaitForProductsFetched()
        {
            if (_service == null)
                throw new InvalidOperationException("[IAP] Not bound.");
            return _service.WaitForProductsFetched();
        }

    }
}