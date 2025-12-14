using System;
using Cysharp.Threading.Tasks;

namespace AES.Tools
{
    public static class IAP
    {
        private static IIap _service;

        internal static void Bind(IIap service) => _service = service;

        public static bool IsBound => _service != null;
        public static bool IsReady => _service?.IsReady ?? false;

        public static UniTask PurchaseByProductKeyAsync(string productKey)
        {
            if (_service == null)
                throw new InvalidOperationException("[IAP] Not bound.");
            return _service.PurchaseByProductKeyAsync(productKey);
        }

        public static UniTask RestoreAsync()
            => _service?.RestoreAsync() ?? UniTask.CompletedTask;
    }
}