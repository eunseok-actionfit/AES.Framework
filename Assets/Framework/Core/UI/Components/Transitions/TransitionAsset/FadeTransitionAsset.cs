using System.Threading;
using AES.Tools.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Components.Transitions.TransitionAsset
{
    [CreateAssetMenu(menuName="UI/Transition/Fade")]
    public sealed class FadeTransitionAsset : TransitionAsset
    {
        [Range(0,2f)] public float duration = 0.25f;

        public override async UniTask In(IUIView view, CancellationToken ct) {
            var cg = view.CanvasGroup;
            float t = 0f;
            while (t < duration) {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.InverseLerp(0, duration, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            cg.alpha = 1f;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct) {
            var cg = view.CanvasGroup;
            float t = 0f;
            float start = cg.alpha;
            while (t < duration) {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(start, 0f, t / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            cg.alpha = 0f;
        }
    }
}


