using System;
using System.Threading;
using AES.Tools.Core.View;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Transitions.Util
{
    internal static class TransitionUtil
    {
        public async static UniTask Loop01(float duration, CancellationToken ct, Action<float> onStep)
        {
            if (duration <= 0f) { onStep?.Invoke(1f); return; }
            float t = 0f;
            while (t < duration)
            {
                ct.ThrowIfCancellationRequested();
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                onStep?.Invoke(u);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
            onStep?.Invoke(1f);
        }

        public static RectTransform Rect(UIView v) => v.GetComponent<RectTransform>();
        public static CanvasGroup CG(UIView v) => v.CanvasGroup;
    }
}