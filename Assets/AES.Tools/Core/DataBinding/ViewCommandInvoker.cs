namespace AES.Tools
{
    public class ViewCommandInvoker : ContextBindingBase
    {
        ICommand _command;

        protected override void OnContextAvailable(IBindingContext ctx, string path)
        {
            _command = ctx.GetValue(path) as ICommand;
        }

        protected override void OnContextUnavailable()
        {
            _command = null;
        }

        public void Invoke()
        {
            if (_command == null) return;
            if (!_command.CanExecute(null)) return;

            _command.Execute(null);
        }
    }
}