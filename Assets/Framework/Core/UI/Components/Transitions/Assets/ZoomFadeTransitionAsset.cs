using System.Threading;
using AES.Tools.Util;
using AES.Tools.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Assets
{
    [CreateAssetMenu(menuName="UI/Transition/ZoomFade")]
    public sealed class ZoomFadeTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.3f;
        public float startScale = 0.9f;

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var tf = view.Rect;
            var cg = view.CanvasGroup;
            var orig = tf.localScale; tf.localScale = Vector3.one * startScale; cg.alpha = 0f;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseOutCubic(u);
                tf.localScale = Vector3.LerpUnclamped(Vector3.one * startScale, orig, e);
                cg.alpha = e;
            });
            tf.localScale = orig; cg.alpha = 1f;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var tf = view.Rect;
            var cg = view.CanvasGroup;
            var orig = tf.localScale; cg.alpha = 1f;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseInOutQuad(u);
                tf.localScale = Vector3.LerpUnclamped(orig, Vector3.one * startScale, e);
                cg.alpha = 1f - e;
            });
            tf.localScale = orig; cg.alpha = 0f;
        }
    }
}