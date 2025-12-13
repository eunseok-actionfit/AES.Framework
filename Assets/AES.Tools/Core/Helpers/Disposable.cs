using System;


namespace AES.Tools
{
    public sealed class Disposable : IDisposable
    {
        private Action _onDispose;

        private Disposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public static IDisposable Create(Action onDispose)
        {
            return new Disposable(onDispose);
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }
    }
}