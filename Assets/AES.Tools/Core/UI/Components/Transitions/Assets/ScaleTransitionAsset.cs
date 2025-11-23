using System.Threading;
using AES.Tools.Util;
using AES.Tools.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Assets
{
    [CreateAssetMenu(menuName="UI/Transition/Scale")]
    public sealed class ScaleTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.25f;
        public float startScale = 0.85f;
        public bool easeOutBack = true;

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var orig = rt.localScale;
            rt.localScale = Vector3.one * startScale;

            await TransitionUtil.Loop01(duration, ct, u =>
            {
                float e = easeOutBack ? Easing.EaseOutBack(u) : u;
                rt.localScale = Vector3.LerpUnclamped(Vector3.one * startScale, orig, e);
            });
            rt.localScale = orig;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            var orig = rt.localScale;
            await TransitionUtil.Loop01(duration, ct, u =>
            {
                float e = easeOutBack ? Easing.EaseOutBack(u) : u;
                rt.localScale = Vector3.LerpUnclamped(orig, Vector3.one * startScale, e);
            });
            rt.localScale = orig;
        }
    }
}