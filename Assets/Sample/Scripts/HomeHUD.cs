using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;


namespace Sample.Scripts
{
    public class HomeHUD : UIView
    {
        private ToastService toast = new ToastService();
        
        public void ShowToast()
        {
            toast.ShowAsync("Hello World!").Forget();
        }
    }
}


