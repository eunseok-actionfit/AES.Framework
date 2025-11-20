using System.Threading;
using AES.Tools.Components.Transitions.TransitionAsset.Util;
using AES.Tools.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Components.Transitions.TransitionAsset
{
    [CreateAssetMenu(menuName = "UI/Transition/Slide")]
    public sealed class SlideTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.35f;
        public Vector2 direction = new Vector2(1f, 0f);
        public bool useEaseOut = true;

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var size = rt.rect.size;
            var start = direction * size;
            var end = Vector2.zero;
            var orig = rt.anchoredPosition;
            rt.anchoredPosition = orig + start;

            await TransitionUtil.Loop01(duration, ct, u => {
                float e = useEaseOut ? Easing.EaseOutCubic(u) : u;
                rt.anchoredPosition = Vector2.LerpUnclamped(orig + start, orig + end, e);
            });

            rt.anchoredPosition = orig;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var size = rt.rect.size;
            var end = direction * size;
            var orig = rt.anchoredPosition;

            await TransitionUtil.Loop01(duration, ct, u => {
                float e = useEaseOut ? Easing.EaseOutCubic(u) : u;
                rt.anchoredPosition = Vector2.LerpUnclamped(orig, orig + end, e);
            });

            rt.anchoredPosition = orig;
        }
    }
}