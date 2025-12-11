using System;


namespace AES.Tools.Commands
{
    public sealed class Command : CommandBase<Unit>
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public Command(
            Action execute,
            Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (() => true);
        }

        public override bool CanExecute(Unit _ = default)
            => _canExecute();

        public override void Execute(Unit unit = default)
        {
            
            if (CanExecute(unit))
                _execute?.Invoke();
        }

        public void RaiseCanExecuteChanged()
        {
            base.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// T 파라미터 기반 ObservableCommand (타입 안전)
    /// </summary>
    public class Command<T> : CommandBase<T>
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public Command(
            Action<T> execute,
            Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (_ => true);
        }

        public override bool CanExecute(T parameter = default)
            => _canExecute(parameter);

        public override void Execute(T parameter = default)
        {
            if (CanExecute(parameter))
                _execute(parameter);
        }
    }
}
