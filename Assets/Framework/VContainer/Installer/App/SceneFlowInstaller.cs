using VContainer.Unity;


namespace VContainer.Installer.App
{
    public sealed class SceneFlowInstaller : IInstaller
    {
        public void Install(IContainerBuilder b)
        {
           // b.Register<ISceneFlow, SceneFlowService>(Lifetime.Singleton);
        }
    }
}