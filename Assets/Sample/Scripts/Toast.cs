using System.Threading;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;


namespace Sample.Scripts
{
    public class Toast : UIView
    {
        public Text text;
        
        protected override UniTask OnShow(object model, CancellationToken ct)
        {
            
            text.text = model.ToString();
            return UniTask.CompletedTask;
        }
    }
}


