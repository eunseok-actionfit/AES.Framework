namespace Core.Systems.UI.Guards
{
    public interface IUiLockService
    {
        bool IsLocked { get; }
        void Lock();
        void Unlock();
        System.IDisposable ScopedLock();
    }
}