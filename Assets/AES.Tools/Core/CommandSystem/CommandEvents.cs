using AES.Tools;

namespace AES.Tools.TBC.CommandSystem
{
    public struct CommandStartedEvent : IEvent
    {
        public IGameCommand Command;
        public string Name;
    }

    public struct CommandCompletedEvent : IEvent
    {
        public IGameCommand Command;
        public string Name;
        public Result Result;
    }

    public struct CommandUndoEvent : IEvent
    {
        public IUndoableGameCommand Command;
        public string Name;
        public Result Result;
    }

    public struct CommandRedoEvent : IEvent
    {
        public IUndoableGameCommand Command;
        public string Name;
        public Result Result;
    }

    public struct CommandFailedEvent : IEvent
    {
        public IGameCommand Command;
        public string Name;
        public string Reason;
    }
}