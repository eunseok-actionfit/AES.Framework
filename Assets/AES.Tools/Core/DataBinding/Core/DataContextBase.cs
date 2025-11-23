using UnityEngine;


namespace AES.Tools
{
    [DefaultExecutionOrder(-1)]
    public abstract class DataContextBase : MonoBehaviour
    {
        public object ViewModel { get; protected set; }

        protected virtual void Awake()
        {
            if (ViewModel == null)
                ViewModel = CreateViewModel();
        }
        
        protected abstract object CreateViewModel();
    }
}


