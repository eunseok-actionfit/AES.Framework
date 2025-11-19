namespace Core.Systems.UI.Guards
{
    public interface IInputGuard
    {
        bool Throttle(string id, float seconds = 0.3f);
        (bool canProcess, System.Action complete) Debounce(string id);
        void Reset();
    }
}
