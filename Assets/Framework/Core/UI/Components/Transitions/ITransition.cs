using System.Threading;
using AES.Tools.Core.View;
using Cysharp.Threading.Tasks;


namespace AES.Tools.Transitions
{
    public interface ITransition
    {
        UniTask In(IUIView view, CancellationToken ct);
        UniTask Out(IUIView view, CancellationToken ct);
    }
}

