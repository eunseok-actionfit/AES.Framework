using Cysharp.Threading.Tasks;


namespace AES.Tools.Commands
{
    public interface IAsyncCommand : ICommand
    {
        UniTask ExecuteAsync(object parameter = null);
    }
    
    public interface IAsyncCommand<T> : ICommand<T>, IAsyncCommand
    {
        UniTask ExecuteAsync(T parameter);
    }
}