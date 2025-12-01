using System;


namespace AES.Tools.Commands
{
    /// <summary>
    /// object 파라미터 기반 Undo 가능한 동기 커맨드
    /// 실행/Undo 둘 다 델리게이트로 구성
    /// </summary>
    public sealed class UndoableCommand : IUndoableCommand
    {
        private readonly Action<object> _execute;
        private readonly Action _undo;
        private readonly Func<object, bool> _canExecute;

        public UndoableCommand(
            Action<object> execute,
            Action undo,
            Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _undo = undo ?? throw new ArgumentNullException(nameof(undo));
            _canExecute = canExecute ?? (_ => true);
        }

        public bool CanExecute(object parameter = null) => _canExecute(parameter);

        public void Execute(object parameter = null)
        {
            if (CanExecute(parameter))
                _execute(parameter);
        }

        public void Undo()
        {
            _undo();
        }
    }

    /// <summary>
    /// 제네릭 버전 (타입 안전)
    /// </summary>
    public sealed class UndoableCommand<T> : IUndoableCommand
    {
        private readonly Action<T> _execute;
        private readonly Action _undo;
        private readonly Func<T, bool> _canExecute;

        public UndoableCommand(
            Action<T> execute,
            Action undo,
            Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _undo = undo ?? throw new ArgumentNullException(nameof(undo));
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
            T value = parameter is T t ? t : default;
            if (_canExecute(value))
                _execute(value);
        }

        public void Undo()
        {
            _undo();
        }
    }
}
