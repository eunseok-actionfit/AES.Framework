using System.Threading;
using AES.Tools.Components.Transitions.TransitionAsset.Util;
using AES.Tools.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Components.Transitions.TransitionAsset
{
    [CreateAssetMenu(menuName="UI/Transition/Flip")]
    public sealed class FlipTransitionAsset : TransitionAsset
    {
        public enum Axis { X, Y }
        [Range(0, 2f)] public float duration = 0.5f;
        public Axis axis = Axis.Y;
        public bool fadeWithFlip = true;

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var cg = view.CanvasGroup;
            Quaternion start = axis == Axis.Y ? Quaternion.Euler(0, 90f, 0) : Quaternion.Euler(90f, 0, 0);
            Quaternion end = Quaternion.identity;
            rt.rotation = start; if (fadeWithFlip) cg.alpha = 0f;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseOutCubic(u);
                rt.rotation = Quaternion.SlerpUnclamped(start, end, e);
                if (fadeWithFlip) cg.alpha = e;
            });
            rt.rotation = end; if (fadeWithFlip) cg.alpha = 1f;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var cg = view.CanvasGroup;
            Quaternion start = rt.rotation;
            Quaternion end = start * (axis == Axis.Y ? Quaternion.Euler(0, 90f, 0) : Quaternion.Euler(90f, 0, 0));
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseInOutQuad(u);
                rt.rotation = Quaternion.SlerpUnclamped(start, end, e);
                if (fadeWithFlip) cg.alpha = 1f - e;
            });
            rt.rotation = end; if (fadeWithFlip) cg.alpha = 0f;
        }
    }
}