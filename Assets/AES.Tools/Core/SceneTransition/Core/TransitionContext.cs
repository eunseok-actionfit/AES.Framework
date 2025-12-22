using UnityEngine.SceneManagement;

public sealed class TransitionContext
{
    public LoadRequest Request;
    public ISceneLoader Loader;
    public IGates Gates;
    
    public LoadingScreenKey LoadingKey;
    public ILoadingPresenter LoadingPresenter;

    public SceneHandle DestinationHandle;
}