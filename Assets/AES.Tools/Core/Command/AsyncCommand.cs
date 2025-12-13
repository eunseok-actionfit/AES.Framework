using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{



    /// <summary>
    /// object 파라미터 기반 비동기 커맨드
    /// </summary>
    public class AsyncCommand : IAsyncCommand
    {
        private readonly Func<object, Task> _executeAsync;
        private readonly Func<object, bool> _canExecute;

        public event Action CanExecuteChanged = delegate { };

        public AsyncCommand(
            Func<object, Task> executeAsync,
            Func<object, bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
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

        public void RaiseCanExecuteUpdated() => CanExecuteChanged();
    }

    /// <summary>
    /// T 파라미터 기반 비동기 커맨드
    /// </summary>
    public class AsyncCommand<T> : IAsyncCommand<T>
    {
        private readonly Func<T, Task> _executeAsync;
        private readonly Func<T, bool> _canExecute;

        public event Action CanExecuteChanged = delegate { };

        public AsyncCommand(
            Func<T, Task> executeAsync,
            Func<T, bool> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute ?? (_ => true);
        }

        public bool CanExecute(T parameter) => _canExecute(parameter);

        public void Execute(T parameter)
        {
            _ = ExecuteAsync(parameter);
        }
        
        async UniTask IAsyncCommand.ExecuteAsync(object parameter)
        {
            if (parameter is T t)
                await ExecuteAsync(t);
        }

        public async UniTask ExecuteAsync(T parameter)
        {
            if (CanExecute(parameter))
                await _executeAsync(parameter);
        }

        bool ICommand.CanExecute(object parameter)
        {
            if (parameter is T t)
                return CanExecute(t);

            return false;
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter is T t)
                Execute(t);
        }

        public void RaiseCanExecuteUpdated() => CanExecuteChanged();
    }
}
