using Cysharp.Threading.Tasks;
using VContainer.Unity;
using AES.Tools.VContainer.Bootstrap.Framework;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Game/Bootstrap/Features/AppOpen Hold",
    fileName = "AppOpenHoldFeature")]
public sealed class AppOpenHoldFeature : AppFeatureSO
{
    public float HoldSeconds = 1.5f;

    public override async UniTask Initialize(LifetimeScope root, FeatureContext ctx)
    {
        // 시작
        ctx.Progress?.Report(0f, $"Hold {HoldSeconds:0.0}s");

        var ms = Mathf.Max(0, (int)(HoldSeconds * 1000));
        if (ms > 0)
            await UniTask.Delay(ms);

        // 완료
        ctx.Progress?.Report(1f, "Hold done");
    }
}