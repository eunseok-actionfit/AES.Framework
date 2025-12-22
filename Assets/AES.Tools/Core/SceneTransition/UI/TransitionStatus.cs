public enum TransitionStatus
{
    None,

    LoadStarted,

    InputBlocked,

    BeforeEntryFade,
    EntryFade,
    AfterEntryFade,

    UnloadOriginScene,
    LoadDestinationScene,
    LoadProgressComplete,

    WaitingForServer,
    CleaningCache,

    BeforeSceneActivation,
    DestinationSceneActivation,
    AfterSceneActivation,

    ExitFade,
    InputUnblocked,

    UnloadLoadingScreen,

    Failed,  

    Complete
}