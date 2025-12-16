using System;


namespace AES.Tools.VContainer.Services.Loading
{
    public interface ILoadingBus
    {
        event Action<float> Progress;
        event Action<string> Message;
        void Report(float v);
        void Say(string message);
        void Reset();
    }
}