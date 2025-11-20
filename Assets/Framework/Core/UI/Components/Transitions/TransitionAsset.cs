using System.Threading;
using AES.Tools.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Components.Transitions.TransitionAsset
{
    public abstract class TransitionAsset : ScriptableObject, ITransition {
        public abstract UniTask In(IUIView view, CancellationToken ct);
        public abstract UniTask Out(IUIView view, CancellationToken ct);
    }
}


