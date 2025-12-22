using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class AntiSpillPrepareStep : ITransitionStep
{
    private readonly AntiSpill _antiSpill;
    private readonly CleanupPlan _plan;
    private readonly string _spillSceneName;

    public AntiSpillPrepareStep(AntiSpill antiSpill, CleanupPlan plan, string spillSceneName = null)
    {
        _antiSpill = antiSpill;
        _plan = plan;
        _spillSceneName = spillSceneName;
    }

    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        _antiSpill.Prepare(_spillSceneName);
        _plan.AntiSpill = _antiSpill;
        _plan.AntiSpillPrepared = true;
        return UniTask.CompletedTask;
    }
}