namespace AES.Tools
{
    public interface ICommandHistory
    {
        void PushDone(IUndoableGameCommand cmd);
        bool TryPopUndo(out IUndoableGameCommand cmd);
        void PushUndone(IUndoableGameCommand cmd);
        bool TryPopRedo(out IUndoableGameCommand cmd);
        void ClearRedo();
        void ClearAll();
        int UndoCount { get; }
        int RedoCount { get; }
    }
}