using System.Threading;
using AES.Tools.Core.View;
using AES.Tools.Transitions.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Transitions.Assets
{
    [CreateAssetMenu(menuName = "UI/Transition/Pop")]
    public sealed class PopTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.28f;
        public float startScale = 0.7f;

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var tf = view.Rect;
            var orig = tf.localScale;
            tf.localScale = Vector3.one * startScale;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseOutBack(u, 1.9f);
                tf.localScale = Vector3.LerpUnclamped(Vector3.one * startScale, orig, e);
            });

            tf.localScale = orig;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var tf = view.Rect;
            var orig = tf.localScale;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseInOutQuad(u);
                tf.localScale = Vector3.LerpUnclamped(orig, Vector3.one * startScale, e);
            });

            tf.localScale = orig;
        }
    }
}