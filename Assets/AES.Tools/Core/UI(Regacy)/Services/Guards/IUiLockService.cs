namespace AES.Tools.UI_Regacy_.Services.Guards
{
    public interface IUiLockService
    {
        bool IsLocked { get; }
        void Lock();
        void Unlock();
        System.IDisposable ScopedLock();
    }
}