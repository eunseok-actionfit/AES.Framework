using UnityEngine.SceneManagement;

public sealed class CleanupPlan
{
    // loading screen handle (Addressables/Unity)
    public SceneHandle LoadingHandle;
    public bool LoadingLoaded;

    public SceneHandle DestinationHandle;
    public bool DestinationLoaded;

    public AntiSpill AntiSpill;
    public bool AntiSpillPrepared;

    public Scene PreviousActiveScene;
    public bool RestorePreviousActiveScene;
}