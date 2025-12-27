using System;

public static class TransitionEventsExtensions
{
    public static IDisposable Once(this TransitionEvents e, TransitionStatus target, Action action)
    {
        void Handler(TransitionStatus s)
        {
            if (s != target) return;
            e.OnStatus -= Handler;
            action?.Invoke();
        }

        e.OnStatus += Handler;
        return new DisposeAction(() => e.OnStatus -= Handler);
    }

    private sealed class DisposeAction : IDisposable
    {
        private Action _dispose;
        public DisposeAction(Action dispose) => _dispose = dispose;
        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }
}