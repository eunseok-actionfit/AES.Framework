using UnityEngine;


namespace AES.Tools.VContainer.AppLifetime
{
    [DisallowMultipleComponent]
    public class ApplicationLifetimeAdapter : PersistentSingleton<ApplicationLifetimeAdapter>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }
        
        private void OnApplicationFocus(bool focus)
        {
            EventBus<ApplicationFocusChangedEvent>.Raise(
                new ApplicationFocusChangedEvent { Focused = focus }
            );
        }

        private void OnApplicationPause(bool paused)
        {
            EventBus<ApplicationPauseChangedEvent>.Raise(
                new ApplicationPauseChangedEvent { Paused = paused }
            );
        }

        private void OnApplicationQuit()
        {
            EventBus<ApplicationQuitEvent>.Raise(
                new ApplicationQuitEvent()
            );
        }
    }
}