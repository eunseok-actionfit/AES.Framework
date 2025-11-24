using AES.Tools.Commands;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public interface IUndoableAsyncCommand : IAsyncCommand
    {
        UniTask UndoAsync();
    }
}


