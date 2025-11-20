using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public sealed class ButtonVM
    {
        public Bindable<bool> IsEnabled { get; }
        public System.Func<UniTask> Command { get; }
        public string SeKey { get; }
        
        public ButtonVM(System.Func<UniTask> command, string seKey = null)
        {
            IsEnabled = new Bindable<bool>(true); 
            Command   = command;
            SeKey     = seKey;
        }
        
        public ButtonVM(Bindable<bool> isEnabled, System.Func<UniTask> command, string seKey = null)
        {
            IsEnabled = isEnabled ?? new Bindable<bool>();
            Command   = command;
            SeKey     = seKey;
        }
        
    }
}
