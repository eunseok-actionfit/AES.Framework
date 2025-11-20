using System.Collections.Generic;


namespace AES.Tools
{
    public sealed class CommandHistory : ICommandHistory
    {
        readonly Stack<ICommand> _undo = new();
        readonly Stack<ICommand> _redo = new();

        public int UndoCount => _undo.Count;
        public int RedoCount => _redo.Count;

        public void PushDone(ICommand c) => _undo.Push(c);
        public bool TryPopUndo(out ICommand c) => _undo.TryPop(out c);
        public void PushUndone(ICommand c) => _redo.Push(c);
        public bool TryPopRedo(out ICommand c) => _redo.TryPop(out c);
        public void ClearRedo() => _redo.Clear();

        public void ClearAll()
        {
            _undo.Clear();
            _redo.Clear();
        }
    }
}