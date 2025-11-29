// using System;
// using UnityEngine;
//
//
// namespace AES.Tools.Sample
// {
//     public class PlayerMonoContext : MonoContextHolder
//     {
//         [SerializeField] PlayerConfig config;
//
//         public override Type ViewModelType => typeof(PlayerViewModel);
//         
//         protected override object CreateViewModel()
//         {
//             if(ViewModel != null) return ViewModel;
//             return new PlayerViewModel(config);
//         }
//     }
// }