using System;


namespace VContainer.AppLifetime
{
    public interface IApplicationLifetime
    {
        event Action<bool> OnFocusChanged;
        event Action<bool> OnPausedChanged;
        event Action       OnQuit;
    }
}