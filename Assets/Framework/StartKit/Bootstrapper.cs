using Application.Config;
using UnityEngine;


namespace StartKit
{
    public class Bootstrapper : PersistentSingleton<Boostrapper>
    {
        [SerializeField] AppConfig appConfig;

        protected override void Awake()
        {
            base.Awake();
            InputStartKit.Initialize(appConfig.inputConfig)
            UIStartKit.Initialize(appConfig.uiRegistrySO)
        }

        private void Update()
        {
            InputServiceLocator.Service.Tick(Time.unscaleDeltaTime)
        }
    }
}


