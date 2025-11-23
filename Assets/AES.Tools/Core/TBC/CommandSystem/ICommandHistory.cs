namespace AES.Tools.TBC.CommandSystem
{
    public interface ICommandHistory
    {
        void PushDone(ICommand cmd);
        bool TryPopUndo(out ICommand cmd);
        void PushUndone(ICommand cmd);
        bool TryPopRedo(out ICommand cmd);
        void ClearRedo();
        void ClearAll();
        int UndoCount { get; }
        int RedoCount { get; }
    }
}