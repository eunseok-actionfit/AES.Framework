using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    // UnityIapService -> (Router) -> Flow 로 중계해서 순환 의존성 제거
    public sealed class PurchaseProcessorRouter : IIapPurchaseProcessor
    {
        public IIapPurchaseProcessor Target { get; set; }

        public UniTask<bool> ProcessAsync(IapPurchaseContext ctx)
            => Target.ProcessAsync(ctx);
    }
}