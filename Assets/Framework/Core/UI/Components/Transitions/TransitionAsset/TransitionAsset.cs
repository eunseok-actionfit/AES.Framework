using System.Threading;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace Core.Systems.UI.Components.Transitions.TransitionAsset
{
    public abstract class TransitionAsset : ScriptableObject, ITransition {
        public abstract UniTask In(IUIView view, CancellationToken ct);
        public abstract UniTask Out(IUIView view, CancellationToken ct);
    }
}


