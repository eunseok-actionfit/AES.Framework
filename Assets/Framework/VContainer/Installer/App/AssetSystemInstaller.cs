using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer.Installer.App
{
    /// 컴포지션 루트에서: new AssetSystemInstaller(assetsConfig).Install(builder);
    public sealed class AssetSystemInstaller : IInstaller
    {
        // private readonly AssetsConfigSO _config;
        // public AssetSystemInstaller(AssetsConfigSO config) { _config = config; }

        public void Install(IContainerBuilder builder)
        {
            // // 구현 등록
            // builder.Register<AddressablesAssetLoader>(Lifetime.Singleton);
            // builder.Register<ResourcesAssetLoader>(Lifetime.Singleton);
             builder.Register<IAssetProvider, AddressablesAssetProvider>(Lifetime.Singleton);
             builder.Register<ISceneLoader, AddressablesSceneLoader>(Lifetime.Singleton);
            //
            // // 파사드 등록
            // builder.Register(r => new AssetProviderFacade(
            //             _config,
            //             r.Resolve<AddressablesAssetLoader>(),
            //             r.Resolve<ResourcesAssetLoader>()),
            //         Lifetime.Singleton)
            //     .As<IAssetLoader>()
            //     .As<IAssetInstantiator>()
            //     .As<IAssetPreloader>();
            //
            // builder.Register<IAssetReader, AssetLoaderReader>(Lifetime.Singleton);
        }
    }
}