using System.Threading;
using AES.Tools.Core.View;
using AES.Tools.Transitions.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Transitions.Assets
{
    [CreateAssetMenu(menuName = "UI/Transition/SpringPosition")]
    public sealed class SpringPositionTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.5f;
        public Vector2 offset = new Vector2(-120f, 0f);

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var basePos = rt.anchoredPosition;
            rt.anchoredPosition = basePos + offset;
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.Spring(u, 0.55f, 14f);
                rt.anchoredPosition = Vector2.LerpUnclamped(basePos + offset, basePos, e);
            });

            rt.anchoredPosition = basePos;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var basePos = rt.anchoredPosition;
            await TransitionUtil.Loop01(duration * 0.8f, ct, u => {
                float e = Easing.EaseOutCubic(u);
                rt.anchoredPosition = Vector2.LerpUnclamped(basePos, basePos + offset, e);
            });

            rt.anchoredPosition = basePos;
        }
    }
}