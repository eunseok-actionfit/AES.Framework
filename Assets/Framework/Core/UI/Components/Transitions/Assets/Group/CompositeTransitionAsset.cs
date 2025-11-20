using System.Collections.Generic;
using System.Threading;
using AES.Tools.Core.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Transitions.Assets.Group
{


    [CreateAssetMenu(menuName = "UI/Transition/Composite")]
    public sealed class CompositeTransitionAsset : TransitionAsset
    {
        public List<TransitionAsset> steps = new();

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            foreach (var s in steps)
            {
                if (!s) continue;
                await s.In(view, ct);
            }
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            for (int i = steps.Count - 1; i >= 0; --i)
            {
                var s = steps[i];
                if (!s) continue;
                await s.Out(view, ct);
            }
        }
    }
}