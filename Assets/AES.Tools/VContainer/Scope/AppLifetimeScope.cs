using AES.Tools.StartKit;
using AES.Tools.VContainer.Installer;
using AES.Tools.VContainer.Installer.App;
using UnityEngine;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer.Scope
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AppLifetimeScope : LifetimeScope
    {
        [SerializeField] private AppConfig config;
        
        [Header("Game Installers (게임에서 채우는 슬롯)")]
        [SerializeField] ScriptableInstaller[] installers;
        protected override void Configure(IContainerBuilder builder)
        {
            if (!config)
                throw new System.NullReferenceException("AppConfig 누락");

            builder.RegisterInstance(config);
            
            if (installers != null)
            {
                foreach (var installer in installers)
                {
                    if (!installer) continue;
                    installer.Install(builder);
                }
            }

            // Core (엔진 + 앱)
            new CoreEngineInstaller().Install(builder);
            // Scene Flow (앱)
            new SceneFlowInstaller().Install(builder);

            // Assets (엔진)
            new AssetSystemInstaller().Install(builder);
            
            // Save / Load (앱)
            new SaveAndLoadInstaller(config.storageProfile).Install(builder);

            // Input (엔진 Installer, config 넘김)
            new InputInstaller(config.inputConfig).Install(builder);
            
            // UI (엔진 Installer)
          //  new UIInstaller(config.uiRegistrySO).Install(builder);
            
            // UX (앱 UX)
            new UXServicesInstaller().Install(builder);
        }
    }
}
