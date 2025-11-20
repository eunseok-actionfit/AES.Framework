using System;


namespace VContainer.AppLifetime
{
    //  Namespace Properties ------------------------------

    public class ApplicationLifetime : IApplicationLifetime
    {
        //  Events ----------------------------------------
        public event Action<bool> OnFocusChanged = delegate { };
        public event Action<bool> OnPausedChanged = delegate { };
        public event Action       OnQuit = delegate { };

        //  Properties ------------------------------------

        //  Fields ----------------------------------------

        //  Initialization  -------------------------------
        // public async UniTask InitializeAsync()
        // {
        //     // todo 필요시 초기화. Config/로그 수집등 
        //     await UniTask.CompletedTask;
        // }

        //  Methods ---------------------------------------
        // Unity 이벤트를 Forward하는 API (MonoBehaviour에서 호출)
        public void RaiseFocus(bool focus) => OnFocusChanged.Invoke(focus);
        public void RaisePause(bool paused) => OnPausedChanged.Invoke(paused);
        public void RaiseQuit() => OnQuit.Invoke();
    }
}


