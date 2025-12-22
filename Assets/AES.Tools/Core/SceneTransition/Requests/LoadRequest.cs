using System;
using System.Collections.Generic;

public sealed class LoadRequest
{
    // Destination
    public string DestinationKey;
    public string FallbackDestinationKey = "Lobby";

    // Loading (single default + override)
    public bool ShowLoadingScreen = true;
    public string LoadingKeyOverride; // LoadingCatalog key. null => cfg.DefaultLoading

    // Scene load policy
    public bool LoadAdditive = true;
    public bool ActivateOnLoad = true;

    public GateId ActivationGate = GateId.BeforeActivation;
    public int ActivationGateTimeoutMs = 0;

    public UnloadPolicy UnloadPolicy = UnloadPolicy.AllLoadedScenes;
    public List<string> KeepSceneNames = new();

    // UX
    public ITransitionUI UI;
    public IFader Fader;
    public IInputBlocker InputBlocker;
    public ITransitionEvents Events;
    
    public float BeforeEntryFadeDelay = 0f;
    public float AfterEntryFadeDelay = 0f;
    public float BeforeActivationDelay = 0f;
    public float AfterActivationDelay = 0f;

    public float EntryFadeDuration = 0f;
    public float ExitFadeDuration = 0f;

    public bool UseAntiSpill = true;
    public string AntiSpillSceneName = "AntiSpill";

    // Progress (optional)
    // NOTE: These are not used directly by ProgressSmoother (it expects sinks).
    // Keep for future, but do not pass into ProgressSmoother ctor.
    public bool RealtimeProgress = true;
    public float SmoothedProgress = 0f;
    public Func<float, float> ProgressSpeedFn;

    // Failure / fallback
    public bool EnableFallback = true;
    public bool FallbackUseAntiSpill = true;
    public bool FallbackShowLoadingScreen = true;

    // Cache clear
    public CacheClearMode CacheClearMode = CacheClearMode.DependencyOnly;
    public string CacheClearLabel;

    // Volatile args
    public ISceneArgs SceneArgs;
}