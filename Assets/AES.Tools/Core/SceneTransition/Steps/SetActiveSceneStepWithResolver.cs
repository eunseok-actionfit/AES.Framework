using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public sealed class SetActiveSceneStepWithResolver : ITransitionStep
{
    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        ctx.Request.Events?.Emit(TransitionStatus.DestinationSceneActivation);

        var s = ctx.DestinationHandle.Scene;
        if (s.IsValid() && s.isLoaded) SceneManager.SetActiveScene(s);
        return UniTask.CompletedTask;
    }
}