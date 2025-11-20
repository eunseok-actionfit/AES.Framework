using UnityEngine;


namespace VContainer.AppLifetime
{
    // Unity 메시지 수신용 어댑터 (루트에 1개)
    [DisallowMultipleComponent]
    public class ApplicationLifetimeAdapter : MonoBehaviour
    {
        //  Fields ----------------------------------------
        private ApplicationLifetime _lifetime;

        //  Initialization  -------------------------------
        public void Construct(ApplicationLifetime lifetime)
        {
            _lifetime = lifetime;
        }

        //  Unity Methods   -------------------------------
        private void OnApplicationFocus(bool focus) => _lifetime?.RaiseFocus(focus);
        private void OnApplicationPause(bool paused) => _lifetime?.RaisePause(paused);
        private void OnApplicationQuit() => _lifetime?.RaiseQuit();
    }
}