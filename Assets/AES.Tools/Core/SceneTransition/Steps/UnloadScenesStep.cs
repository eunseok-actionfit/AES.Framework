using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public sealed class UnloadScenesStep : ITransitionStep
{
    public UniTask Execute(TransitionContext ctx, CancellationToken ct)
    {
        ctx.Request.Events?.Emit(TransitionStatus.UnloadOriginScene);

        var req = ctx.Request;
        var list = new System.Collections.Generic.List<Scene>();

        // AntiSpill 씬 이름(있으면) 언로드에서 제외
        var skipSpill = req.UseAntiSpill && !string.IsNullOrEmpty(req.AntiSpillSceneName);
        var spillName = req.AntiSpillSceneName;

        switch (req.UnloadPolicy)
        {
            case UnloadPolicy.None:
                break;

            case UnloadPolicy.ActiveScene:
            {
                var s = SceneManager.GetActiveScene();
                if (s.IsValid() && s.isLoaded)
                {
                    if (!(skipSpill && s.name == spillName))
                        list.Add(s);
                }
                break;
            }

            case UnloadPolicy.AllLoadedScenes:
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (!s.IsValid() || !s.isLoaded) continue;

                    if (req.KeepSceneNames.Contains(s.name)) continue;

                    // AntiSpill 씬은 언로드 대상에서 제외
                    if (skipSpill && s.name == spillName) continue;

                    list.Add(s);
                }
                break;
        }

        return ctx.Loader.UnloadScenesAsync(list, ct);
    }
}