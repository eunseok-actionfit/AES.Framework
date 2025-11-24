using System;
using UnityEngine;


namespace AES.Tools.Sample
{
    public class MasterDataContext : DataContextBase
    {
        [SerializeField] PlayerConfig config;

        public override Type ViewModelType => typeof(MasterViewModel);
        
        protected override object CreateViewModel()
        {
            if(ViewModel != null) return ViewModel;
            return new MasterViewModel(config);
        }
    }
}