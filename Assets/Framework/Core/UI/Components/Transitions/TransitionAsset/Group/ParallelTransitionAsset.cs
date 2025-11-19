using System.Collections.Generic;
using System.Threading;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace Core.Systems.UI.Components.Transitions.TransitionAsset.Group
{


    [CreateAssetMenu(menuName = "UI/Transition/Parallel")]
    public sealed class ParallelTransitionAsset : TransitionAsset
    {
        public List<TransitionAsset> group = new();
        static UniTask WhenAllPreserve(IEnumerable<UniTask> tasks) => UniTask.WhenAll(tasks);

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var list = new List<UniTask>(group.Count);

            foreach (var s in group)
            {
                if (!s) continue;
                list.Add(s.In(view, ct));
            }

            await WhenAllPreserve(list);
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var list = new List<UniTask>(group.Count);

            foreach (var s in group)
            {
                if (!s) continue;
                list.Add(s.Out(view, ct));
            }

            await WhenAllPreserve(list);
        }
    }
}