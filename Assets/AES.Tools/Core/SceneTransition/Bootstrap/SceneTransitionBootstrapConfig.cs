using UnityEngine;

public sealed class SceneTransitionBootstrapConfig
{
    // Default loading key (if LoadRequest.LoadingKeyOverride is null/empty)
    public LoadingScreenKey DefaultLoading = new LoadingScreenKey("LoadingInGame", null, false);

    // Presentation strategy
    public LoadingPresentationMode LoadingMode = LoadingPresentationMode.Overlay;

    // Used when LoadingMode == Overlay
    public GameObject OverlayLoadingPrefab;

    // Defaults
    public string[] KeepSceneNames = { "Persistent" };

    public float EntryFadeDuration = 0f;
    public float ExitFadeDuration = 0f;

    public bool UseAntiSpill = true;
}