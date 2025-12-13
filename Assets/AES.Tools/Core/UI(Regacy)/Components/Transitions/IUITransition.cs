using System.Threading;
using AES.Tools.UI_Regacy_.Core.View;
using Cysharp.Threading.Tasks;


namespace AES.Tools.UI_Regacy_.Components.Transitions
{
    public interface IUITransition
    {
        UniTask In(IUIView view, CancellationToken ct);
        UniTask Out(IUIView view, CancellationToken ct);
    }
}

