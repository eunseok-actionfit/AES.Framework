using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class FadeInStep : ITransitionStep
{
    private readonly IFader _fader;
    private readonly float _duration;
    private readonly TransitionStatus _emit;

    public FadeInStep(IFader fader, float duration, TransitionStatus emit = TransitionStatus.EntryFade)
    {
        _fader = fader;
        _duration = duration;
        _emit = emit;
    }

    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        if (_fader == null || _duration <= 0f) return UniTask.CompletedTask;
        ctx.Request.Events?.Emit(_emit);
        return _fader.FadeIn(_duration, ct);
    }
}