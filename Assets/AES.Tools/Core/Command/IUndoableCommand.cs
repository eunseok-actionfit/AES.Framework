namespace AES.Tools
{
    public interface IUndoableCommand : ICommand
    {
        void Undo();
    }
}


