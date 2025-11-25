using System;
using UnityEngine;


namespace AES.Tools.Sample
{
    public class MasterMonoContext : MonoContext<MasterViewModel>
    {
        [SerializeField] PlayerConfig config;
        
        protected override MasterViewModel CreateViewModelTyped()
        {
            return new MasterViewModel(config);
        }
    }
}