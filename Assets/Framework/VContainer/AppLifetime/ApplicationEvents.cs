namespace AES.Tools.VContainer.AppLifetime
{
    /// <summary>
    /// Application Focus 상태가 변경되었을 때 발생하는 이벤트.
    /// </summary>
    public struct ApplicationFocusChangedEvent : IEvent
    {
        public bool Focused;
    }

    /// <summary>
    /// Application Pause 상태가 변경되었을 때 발생하는 이벤트.
    /// </summary>
    public struct ApplicationPauseChangedEvent : IEvent
    {
        public bool Paused;
    }

    /// <summary>
    /// Application이 종료되기 직전에 발생하는 이벤트.
    /// </summary>
    public struct ApplicationQuitEvent : IEvent
    {
    }
}


