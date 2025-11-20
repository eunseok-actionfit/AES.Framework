using System.Threading;
using AES.Tools.Core;
using Cysharp.Threading.Tasks;


namespace AES.Tools.Components.Transitions
{
    public interface ITransition
    {
        UniTask In(IUIView view, CancellationToken ct);
        UniTask Out(IUIView view, CancellationToken ct);
    }
}

