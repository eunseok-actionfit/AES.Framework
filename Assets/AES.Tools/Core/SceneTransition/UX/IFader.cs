using System.Threading;
using Cysharp.Threading.Tasks;

public interface IFader
{
    // FadeIn: 화면을 가림(검게) / FadeOut: 화면을 밝힘
    UniTask FadeIn(float duration, CancellationToken ct);
    UniTask FadeOut(float duration, CancellationToken ct);
}