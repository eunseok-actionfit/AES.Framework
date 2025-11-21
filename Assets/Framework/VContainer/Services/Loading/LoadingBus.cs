using System;


namespace AES.Tools.VContainer
{
    public sealed class LoadingBus : ILoadingBus
    {
        public event Action<float> Progress;
        public event Action<string> Message;

        public void Report(float v)
        {
            if (v < 0f) v = 0f;
            else if (v > 1f) v = 1f;
            Progress?.Invoke(v);
        }

        public void Say(string message)
        {
            if (!string.IsNullOrEmpty(message)) Message?.Invoke(message);
        }

        public void Reset()
        {
            Progress = null;
            Message = null;
        }
    }
}