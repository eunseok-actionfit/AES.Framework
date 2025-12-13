using System;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    /// <summary>
    /// object 파라미터 기반 Undo 가능한 비동기 커맨드
    /// </summary>
    public sealed class UndoableAsyncCommand : IUndoableAsyncCommand
    {
        private readonly Func<object, UniTask> _executeAsync;
        private readonly Func<UniTask> _undoAsync;
        private readonly Func<object, bool> _canExecute;

        public event Action CanExecuteChanged = delegate { };

        public UndoableAsyncCommand(
            Func<object, UniTask> executeAsync,
            Func<UniTask> undoAsync,
            Func<object, bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _undoAsync = undoAsync ?? throw new ArgumentNullException(nameof(undoAsync));
            _canExecute = canExecute ?? (_ => true);
        }

        public bool CanExecute(object parameter = null) => _canExecute(parameter);

        public void Execute(object parameter = null)
        {
            _ = ExecuteAsync(parameter);
        }

        public async UniTask ExecuteAsync(object parameter = null)
        {
            if (CanExecute(parameter))
                await _executeAsync(parameter);
        }

        public async UniTask UndoAsync()
        {
            await _undoAsync();
        }

        public void RaiseCanExecuteUpdated() => CanExecuteChanged();
    }

    /// <summary>
    /// 제네릭 버전 (타입 안전)
    /// </summary>
    public sealed class UndoableAsyncCommand<T> : IUndoableAsyncCommand
    {
        private readonly Func<T, UniTask> _executeAsync;
        private readonly Func<UniTask> _undoAsync;
        private readonly Func<T, bool> _canExecute;

        public event Action CanExecuteChanged = delegate { };

        public UndoableAsyncCommand(
            Func<T, UniTask> executeAsync,
            Func<UniTask> undoAsync,
            Func<T, bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _undoAsync = undoAsync ?? throw new ArgumentNullException(nameof(undoAsync));
            _canExecute = canExecute ?? (_ => true);
        }

        public bool CanExecute(object parameter = null)
        {
            if (parameter is T t)
                return _canExecute(t);

            return _canExecute(default);
        }

        public void Execute(object parameter = null)
        {
            _ = ExecuteAsync(parameter);
        }

        public async UniTask ExecuteAsync(object parameter = null)
        {
            T value = parameter is T t ? t : default;
            if (_canExecute(value))
                await _executeAsync(value);
        }

        public async UniTask UndoAsync()
        {
            await _undoAsync();
        }

        public void RaiseCanExecuteUpdated() => CanExecuteChanged();
    }
}
