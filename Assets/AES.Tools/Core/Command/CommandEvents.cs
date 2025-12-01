namespace AES.Tools.Commands
{
    public struct CommandStartedEvent : IEvent
    {
        public object Command;
        public string Name;
    }

    public struct CommandCompletedEvent : IEvent
    {
        public object Command;
        public string Name;
    }

    public struct CommandUndoEvent : IEvent
    {
        public object Command;
        public string Name;
    }

    public struct CommandRedoEvent : IEvent
    {
        public object Command;
        public string Name;
    }

    public struct CommandFailedEvent : IEvent
    {
        public object Command;
        public string Name;
        public string Reason;
    }
}