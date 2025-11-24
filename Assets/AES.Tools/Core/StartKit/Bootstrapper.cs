// using UnityEngine;
//
//
// namespace AES.Tools.StartKit
// {
//     public class Bootstrapper : PersistentSingleton<Bootstrapper>
//     {
//         [SerializeField] AppConfig appConfig;
//
//         protected override void Awake()
//         {
//             base.Awake();
//             InputStartKit.Initialize(appConfig.inputConfig);
//             UIStartKit.Initialize(appConfig.uiRegistrySO);
//         }
//
//         private void Update()
//         {
//             InputServiceLocator.Service.Tick(Time.unscaledDeltaTime);
//         }
//     }
// }
//
//
