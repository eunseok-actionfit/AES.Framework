using System;
using Unity.Android.Gradle;
using UnityEngine;
using VContainer;


namespace AES.Tools.Sample
{
    public class PlayerDataContext : DataContextBase
    {
        [SerializeField] PlayerConfig config;

        public override Type ViewModelType => typeof(PlayerViewModel);

        [Inject]
        public void Construct(PlayerViewModel vm)
        {
            ViewModel = vm;
        }

        protected override object CreateViewModel()
        {
            if(ViewModel != null) return ViewModel;
            return new PlayerViewModel(config);
        }
    }
}