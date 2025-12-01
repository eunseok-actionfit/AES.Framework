using System;


namespace AES.Tools
{
    public sealed class Subscription : IDisposable
    {
        private Action _dispose;
        private bool _isDisposed;

        public Subscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _dispose?.Invoke();
            _dispose = null;
            _isDisposed = true;
        }
    }
}