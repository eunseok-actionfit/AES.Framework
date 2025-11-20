using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;


namespace AES.Tools.VContainer
{
    [CreateAssetMenu(menuName = "Game/Modules/FrameRate Module", fileName = "FrameRateModule")]
    public sealed class FrameRateBootstrapModule : BootstrapModule
    {
        public int targetFrameRate = 60;
        public bool disableVSync = true;

        public override UniTask Initialize(LifetimeScope root)
        {
            if (disableVSync)
                QualitySettings.vSyncCount = 0;

            Application.targetFrameRate = targetFrameRate;

            return UniTask.CompletedTask;
        }
    }
}