using System.Collections.Generic;
using System.Threading;
using AES.Tools.Components.Transitions.TransitionAsset.Util;
using AES.Tools.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools.Components.Transitions.TransitionAsset
{
    [CreateAssetMenu(menuName = "UI/Transition/ColorTint")]
    public sealed class ColorTintTransitionAsset : TransitionAsset
    {
        [Range(0, 2f)] public float duration = 0.25f;
        public Color from = new Color(1, 1, 1, 0);

        public override async UniTask In(IUIView view, CancellationToken ct)
        {
            var graphics = view.Rect.GetComponentsInChildren<Graphic>(true);
            var orig = new Dictionary<Graphic, Color>(graphics.Length);

            foreach (var g in graphics)
            {
                orig[g] = g.color;
                g.color = from;
            }

            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseOutCubic(u);
                foreach (var g in graphics)
                    if (g)
                        g.color = Color.LerpUnclamped(from, orig[g], e);
            });

            foreach (var g in graphics)
                if (g)
                    g.color = orig[g];
        }

        public override async UniTask Out(IUIView view, CancellationToken ct)
        {
            var graphics = view.Rect.GetComponentsInChildren<Graphic>(true);
            var orig = new Dictionary<Graphic, Color>(graphics.Length);

            foreach (var g in graphics) { orig[g] = g.color; }

            await TransitionUtil.Loop01(duration, ct, u => {
                float e = Easing.EaseInOutQuad(u);
                foreach (var g in graphics)
                    if (g)
                        g.color = Color.LerpUnclamped(orig[g], from, e);
            });

            foreach (var g in graphics)
                if (g)
                    g.color = orig[g];
        }
    }
}