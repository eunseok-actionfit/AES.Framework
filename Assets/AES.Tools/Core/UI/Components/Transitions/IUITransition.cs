using System.Threading;
using AES.Tools.View;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public interface IUITransition
    {
        UniTask In(IUIView view, CancellationToken ct);
        UniTask Out(IUIView view, CancellationToken ct);
    }
}

