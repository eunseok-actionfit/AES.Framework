using System.Threading;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace Core.Systems.UI.Components.Transitions.TransitionAsset
{
    [CreateAssetMenu(menuName="UI/Transition/Delay")]
    public sealed class DelayTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float delay = 0.15f;
        public override async UniTask In(IUIView view, CancellationToken ct)  { await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: ct); }
        public override async UniTask Out(IUIView view, CancellationToken ct) { await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: ct); }
    }
}