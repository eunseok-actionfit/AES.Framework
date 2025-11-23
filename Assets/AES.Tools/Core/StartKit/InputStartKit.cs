using AES.Tools.Core;
using AES.Tools.Platform;


namespace AES.Tools.StartKit
{
    public static class InputStartKit
    {
        public static void Initialize(InputConfig config)
        {
#if UNITY_STANDALONE
    var sorce = new MousePointerSource();
#else
            var sorce = new TouchPointerSource();
#endif
            var service = new InputService(config, sorce);

            InputServiceLocator.Service = service;
        }
    }
}