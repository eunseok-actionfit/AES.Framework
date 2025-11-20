using AES.Tools.Config;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer.Installer;
using VContainer.Installer.App;
using VContainer.Unity;


namespace VContainer.Scope
{
    [DefaultExecutionOrder(-1000)]
    public sealed class AppLifetimeScope : LifetimeScope
    {
        [SerializeField, Required] private AppConfig config;
        
        protected override void Configure(IContainerBuilder builder)
        {
            if (!config)
                throw new System.NullReferenceException("AppConfig 누락");

            // Core (엔진 + 앱)
            new CoreEngineInstaller().Install(builder);
            new AppCoreInstaller().Install(builder);

            // Assets (엔진)
            //new AssetSystemInstaller(config.assets).Install(builder);

            // Scene Flow (앱)
            new SceneFlowInstaller().Install(builder);

            // Save / Load (앱)
            new SaveAndLoadInstaller().Install(builder);

            // Input (엔진 Installer, config 넘김)
            new InputInstaller(config.inputConfig).Install(builder);
            
            // UI (엔진 Installer)
            new UIInstaller(config.uiRegistrySO).Install(builder);



            // UX (앱 UX)
            new UXServicesInstaller().Install(builder);

           
        }
    }
}
