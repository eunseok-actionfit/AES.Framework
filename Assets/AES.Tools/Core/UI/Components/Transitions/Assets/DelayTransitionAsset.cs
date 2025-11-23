using System.Threading;
using AES.Tools.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Assets
{
    [CreateAssetMenu(menuName="UI/Transition/Delay")]
    public sealed class DelayTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float delay = 0.15f;
        public override async UniTask In(IUIView view, CancellationToken ct)  { await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: ct); }
        public override async UniTask Out(IUIView view, CancellationToken ct) { await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: ct); }
    }
}