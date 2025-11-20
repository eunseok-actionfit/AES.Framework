namespace AES.Tools.Guards
{
    public interface IUiLockService
    {
        bool IsLocked { get; }
        void Lock();
        void Unlock();
        System.IDisposable ScopedLock();
    }
}