using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class UGUICanvasGroupFader : FaderBase
{
    [Header("Assign a full-screen CanvasGroup")]
    public CanvasGroup CanvasGroup;

    [Header("Blocks raycasts during fade")]
    public bool BlockRaycasts = true;

    private void Awake()
    {
        if (CanvasGroup == null) CanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (CanvasGroup != null)
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
        }
    }

    public override UniTask FadeIn(float duration, CancellationToken ct)
        => FadeTo(1f, duration, ct);

    public override  UniTask FadeOut(float duration, CancellationToken ct)
        => FadeTo(0f, duration, ct);

    private async UniTask FadeTo(float target, float duration, CancellationToken ct)
    {
        if (CanvasGroup == null) return;

        if (BlockRaycasts)
        {
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;
        }

        var start = CanvasGroup.alpha;

        if (duration <= 0f)
        {
            CanvasGroup.alpha = target;
            if (Mathf.Approximately(target, 0f))
            {
                CanvasGroup.blocksRaycasts = false;
                CanvasGroup.interactable = false;
            }
            return;
        }

        float t = 0f;
        while (t < duration)
        {
            ct.ThrowIfCancellationRequested();
            t += Time.unscaledDeltaTime;
            var u = Mathf.Clamp01(t / duration);
            CanvasGroup.alpha = Mathf.Lerp(start, target, u);
            await UniTask.Yield();
        }

        CanvasGroup.alpha = target;

        if (Mathf.Approximately(target, 0f))
        {
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
        }
    }
}