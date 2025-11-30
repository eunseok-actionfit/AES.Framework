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
        public abstract bool CanExecute(T parameter= default);
        public abstract void Execute(T parameter= default);

        // ICommand(object) 구현 → UI는 이것만 본다
        bool ICommand.CanExecute(object parameter)
        {
            if (parameter is T t)
                return CanExecute(t);

            // 파라미터가 없거나 타입 안 맞을 때 처리 방식은 프로젝트 스타일에 맞게
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
