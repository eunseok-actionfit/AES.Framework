using UnityEngine;

public sealed class SceneTransitionBootstrapConfig
{
    public LoadingScreenKey DefaultLoading = new LoadingScreenKey("LoadingInGame", null, false);

    public LoadingPresentationMode LoadingMode = LoadingPresentationMode.Overlay;
    public GameObject OverlayLoadingPrefab;

    public string[] KeepSceneNames = { "Persistent" };

    // Fade defaults
    public float EntryFadeDuration = 0f;
    
    public float AfterEntryFadeDelay = 0f;

    public float ExitFadeDuration = 0f;

    public bool UseAntiSpill = true;
}