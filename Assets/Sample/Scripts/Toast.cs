using System.Threading;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;


namespace Sample.Scripts
{
    public struct ToastModel { public string Message; }
    public class Toast : UIView<ToastModel>
    {
        public Text text;

        protected override void Bind(ToastModel model)
        {
            text.text = model.Message;
        }
        
    }
}


