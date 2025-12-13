using AES.Tools.TBC.Result;


namespace AES.Tools
{
   
    public struct SaveStartedEvent : IEvent
    {
        public bool IsSave;
    }

    public struct SaveCompletedEvent : IEvent
    {
        public bool IsSave;
        public Result Result;
    }

    public struct SaveFailedEvent : IEvent
    {
        public bool IsSave;
        public Error Error;
    }

}


