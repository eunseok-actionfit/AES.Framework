using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AntiSpill
{
    public Scene SpillScene { get; private set; }
    private readonly List<GameObject> _roots = new(64);
    private bool _prepared;
    private string _spillSceneName;

    public void Prepare(string spillSceneName = null)
    {
        _spillSceneName = string.IsNullOrEmpty(spillSceneName) ? "AntiSpill_Temp" : spillSceneName;

        // 이미 준비됐더라도, SpillScene이 invalid/unloaded면 복구해야 함
        if (_prepared && SpillScene.IsValid() && SpillScene.isLoaded)
            return;

        SpillScene = SceneManager.CreateScene(_spillSceneName);
        SceneManager.SetActiveScene(SpillScene);
        _prepared = true;
    }

    public async UniTask FlushToAsync(Scene destinationScene, bool unloadSpillScene, CancellationToken ct)
    {
        if (!_prepared) return;
        if (!destinationScene.IsValid() || !destinationScene.isLoaded) return;

        // 핵심: SpillScene이 invalid면 여기서 복구(또는 return)
        if (!SpillScene.IsValid() || !SpillScene.isLoaded)
        {
            Prepare(_spillSceneName);
            if (!SpillScene.IsValid() || !SpillScene.isLoaded) return; // 그래도 안되면 안전 종료
        }

        _roots.Clear();
        SpillScene.GetRootGameObjects(_roots);

        for (int i = 0; i < _roots.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var go = _roots[i];
            if (go != null) SceneManager.MoveGameObjectToScene(go, destinationScene);
        }

        if (unloadSpillScene && SpillScene.IsValid() && SpillScene.isLoaded)
        {
            var op = SceneManager.UnloadSceneAsync(SpillScene);
            while (op != null && !op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await UniTask.Yield();
            }

            // 선택: 다음 호출에서 재생성될 수 있게
            SpillScene = default;
        }
    }
}