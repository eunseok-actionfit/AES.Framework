using System.Threading;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace Core.Systems.UI.Components.Transitions.TransitionAsset.Group
{
    //  Namespace Properties ------------------------------

    public class CanvasGroupFade : ITransition
    {
        private readonly float _duration;
        public CanvasGroupFade(float duration = 0.15f) => _duration = duration;

        public async UniTask In(IUIView view, CancellationToken ct)
        {
            var cg = view.CanvasGroup;
            float t = 0;
            while (t < _duration) {
                ct.ThrowIfCancellationRequested();
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(t / _duration);
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }
        }

        public async UniTask Out(IUIView view, CancellationToken ct)
        {
            var cg = view.CanvasGroup;
            float t = 0;
            while (t < _duration) {
                ct.ThrowIfCancellationRequested();
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(1f - (t / _duration));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }
            cg.alpha = 0f;
        }
    }
}


