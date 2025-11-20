using System;


namespace AES.Tools.Guards
{
    /// <summary>
    /// 화면 전환/로딩 등 동안 전체 UI 입력을 잠그는 전역 락.
    /// </summary>
    public sealed class UiLockService : IUiLockService
    {
        private int _count;
        public bool IsLocked => _count > 0;
        public void Lock() => _count++;
        public void Unlock() => _count = Math.Max(0, _count - 1);
        public IDisposable ScopedLock() => new Scope(this);
        private sealed class Scope : IDisposable
        {
            private UiLockService _s;
            public Scope(UiLockService s) { _s = s; s.Lock(); }
            public void Dispose() { _s?.Unlock(); _s = null; }
        }
    }
}