using System.Threading;
using Core.Systems.UI.Components.Transitions.TransitionAsset.Util;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace Core.Systems.UI.Components.Transitions.TransitionAsset
{
    [CreateAssetMenu(menuName = "UI/Transition/SlideFade")]
    public sealed class SlideFadeTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.35f;
        public Vector2 direction = new Vector2(0f, -2f);

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var cg = view.CanvasGroup;
            Vector2 size = rt.rect.size;
            Vector2 off = direction * size;
            Vector2 basePos = rt.anchoredPosition;
            rt.anchoredPosition = basePos + off;
            cg.alpha = 0f;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseOutCubic(u);
                rt.anchoredPosition = Vector2.LerpUnclamped(basePos + off, basePos, e);
                cg.alpha = e;
            });

            rt.anchoredPosition = basePos;
            cg.alpha = 1f;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var cg = view.CanvasGroup;
            Vector2 size = rt.rect.size;
            Vector2 off = direction * size;
            Vector2 basePos = rt.anchoredPosition;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseInOutQuad(u);
                rt.anchoredPosition = Vector2.LerpUnclamped(basePos, basePos + off, e);
                cg.alpha = 1f - e;
            });

            rt.anchoredPosition = basePos;
            cg.alpha = 0f;
        }
    }
}