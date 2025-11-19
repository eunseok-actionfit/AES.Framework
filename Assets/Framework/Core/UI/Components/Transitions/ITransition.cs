using System.Threading;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;


namespace Core.Systems.UI.Components.Transitions
{
    public interface ITransition
    {
        UniTask In(IUIView view, CancellationToken ct);
        UniTask Out(IUIView view, CancellationToken ct);
    }
}

