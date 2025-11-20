using System.Threading;
using AES.Tools.Components.Transitions.TransitionAsset.Util;
using AES.Tools.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Components.Transitions.TransitionAsset
{
    [CreateAssetMenu(menuName = "UI/Transition/Rotate")]
    public sealed class RotateTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.5f;
        public float angle = 90f;
        public bool easeOut = true;

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var tf = view.Rect;
            var end = tf.rotation;
            var start = Quaternion.Euler(0, 0, angle) * end;
            tf.rotation = start;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = easeOut ? Easing.EaseOutCubic(u) : u;
                tf.rotation = Quaternion.SlerpUnclamped(start, end, e);
            });

            tf.rotation = end;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var tf = view.Rect;
            var start = tf.rotation;
            var end = Quaternion.Euler(0, 0, angle) * start;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = easeOut ? Easing.EaseOutCubic(u) : u;
                tf.rotation = Quaternion.SlerpUnclamped(start, end, e);
            });

            tf.rotation = start;
        }
    }
}