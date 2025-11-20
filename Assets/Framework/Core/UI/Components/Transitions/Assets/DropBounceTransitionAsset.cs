using System.Threading;
using AES.Tools.Util;
using AES.Tools.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Assets
{
    [CreateAssetMenu(menuName = "UI/Transition/DropBounce")]
    public sealed class DropBounceTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.45f;
        public float overshoot = 40f;

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var basePos = rt.anchoredPosition;
            rt.anchoredPosition = basePos + new Vector2(0, rt.rect.height);
            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.Spring(u, 0.6f, 18f);
                float y = Mathf.LerpUnclamped(basePos.y + rt.rect.height, basePos.y - overshoot, e);
                rt.anchoredPosition = new Vector2(basePos.x, y);
            });

            rt.anchoredPosition = basePos;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var basePos = rt.anchoredPosition;
            await TransitionUtil.Loop01(duration * 0.7f, ct, u => {
                float e = Easing.EaseInOutQuad(u);
                float y = Mathf.LerpUnclamped(basePos.y, basePos.y + rt.rect.height, e);
                rt.anchoredPosition = new Vector2(basePos.x, y);
            });

            rt.anchoredPosition = basePos;
        }
    }
}