using AES.Tools.VContainer.Services;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer.Installer.App
{
    public sealed class SceneFlowInstaller : IInstaller
    {
        public void Install(IContainerBuilder b)
        {
            b.Register<ISceneFlow, SceneFlowService>(Lifetime.Singleton);
        }
    }
}