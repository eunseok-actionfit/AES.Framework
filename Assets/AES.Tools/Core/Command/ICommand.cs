using System;


namespace AES.Tools
{
    public interface ICommand
    {
        bool CanExecute(object parameter = null);
        void Execute(object parameter = null);
    }

    public interface ICommand<in T> : ICommand
    {
        bool CanExecute(T parameter);
        void Execute(T parameter);
    }

    public abstract class CommandBase<T> : ICommand<T>
    {
        public event Action CanExecuteChanged = delegate { };

        protected void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke();
        
        public abstract bool CanExecute(T parameter= default);
        public abstract void Execute(T parameter= default);

        // ICommand(object) 구현 → UI는 이것만 본다
        bool ICommand.CanExecute(object parameter)
        {
            if (parameter is T t)
                return CanExecute(t);

          
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            if (parameter is T t)
                Execute(t);
            else
                Execute();
        }
        
    }
}
