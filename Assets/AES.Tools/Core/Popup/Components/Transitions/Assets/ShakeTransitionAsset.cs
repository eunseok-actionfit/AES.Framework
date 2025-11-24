using System.Threading;
using AES.Tools.Util;
using AES.Tools.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Assets
{
    [CreateAssetMenu(menuName="UI/Transition/Shake")]
    public sealed class ShakeTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.4f;
        public float amplitude = 16f;
        public int vibrato = 20;

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var rt = view.Rect;
            Vector2 orig = rt.anchoredPosition;
            await TransitionUtil.Loop01(duration, ct, u =>
            {
                float damper = 1f - u;
                float x = (Mathf.PerlinNoise(u * vibrato, 0.12f) - 0.5f) * 2f * amplitude * damper;
                float y = (Mathf.PerlinNoise(0.37f, u * vibrato) - 0.5f) * 2f * amplitude * damper;
                rt.anchoredPosition = orig + new Vector2(x, y);
            });
            rt.anchoredPosition = orig;
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            await In(view, ct);
        }
    }
}