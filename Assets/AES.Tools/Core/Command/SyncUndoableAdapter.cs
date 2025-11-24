using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    /// <summary>
    /// 동기 IUndoableCommand를 IUndoableAsyncCommand처럼 쓰기 위한 어댑터
    /// </summary>
    public sealed class SyncUndoableAdapter : IUndoableAsyncCommand
    {
        private readonly IUndoableCommand _inner;

        public SyncUndoableAdapter(IUndoableCommand inner)
        {
            _inner = inner;
        }

        public bool CanExecute(object parameter = null) => _inner.CanExecute(parameter);

        public void Execute(object parameter = null)
        {
            _inner.Execute(parameter);
        }

        public UniTask ExecuteAsync(object parameter = null)
        {
            _inner.Execute(parameter);
            return UniTask.CompletedTask;
        }

        public void Undo()
        {
            _inner.Undo();
        }

        public UniTask UndoAsync()
        {
            _inner.Undo();
            return UniTask.CompletedTask;
        }
    }
}