using System;
using UnityEngine;


namespace AES.Tools.Sample
{
    public class PlayerDataContext : DataContextBase
    {
        [SerializeField] PlayerConfig config;

        public override Type ViewModelType => typeof(PlayerViewModel);

        protected override object CreateViewModel()
        {
            return new PlayerViewModel(config);
        }
    }
}