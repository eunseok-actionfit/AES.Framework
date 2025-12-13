using System.Threading;
using AES.Tools.UI_Regacy_.Core.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.UI_Regacy_.Components.Transitions
{
    public abstract class TransitionAsset : ScriptableObject, IUITransition {
        public abstract UniTask In(IUIView view, CancellationToken ct);
        public abstract UniTask Out(IUIView view, CancellationToken ct);
    }
}


