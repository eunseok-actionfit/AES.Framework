using System.Collections.Generic;


namespace AES.Tools
{
    public sealed class CommandHistory : ICommandHistory
    {
        private readonly Stack<IUndoableGameCommand> _undo = new();
        private readonly Stack<IUndoableGameCommand> _redo = new();

        public int UndoCount => _undo.Count;
        public int RedoCount => _redo.Count;

        public void PushDone(IUndoableGameCommand c) => _undo.Push(c);
        public bool TryPopUndo(out IUndoableGameCommand c) => _undo.TryPop(out c);
        public void PushUndone(IUndoableGameCommand c) => _redo.Push(c);
        public bool TryPopRedo(out IUndoableGameCommand c) => _redo.TryPop(out c);
        public void ClearRedo() => _redo.Clear();

        public void ClearAll()
        {
            _undo.Clear();
            _redo.Clear();
        }
    }
}