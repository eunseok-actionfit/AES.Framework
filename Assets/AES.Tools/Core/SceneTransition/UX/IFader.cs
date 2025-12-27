using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


public interface IFader
{
    // FadeIn: 화면을 가림(검게) / FadeOut: 화면을 밝힘
    UniTask FadeIn(float duration, CancellationToken ct);
    UniTask FadeOut(float duration, CancellationToken ct);
}

public abstract class FaderBase :  MonoBehaviour, IFader
{
    public virtual UniTask FadeIn(float duration, CancellationToken ct) => UniTask.CompletedTask;

    public virtual UniTask FadeOut(float duration, CancellationToken ct) => UniTask.CompletedTask;
}