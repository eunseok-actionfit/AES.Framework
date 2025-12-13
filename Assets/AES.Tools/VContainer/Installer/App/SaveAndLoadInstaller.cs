using VContainer;
using VContainer.Unity;

namespace AES.Tools.VContainer.Installer.App
{
    public class SaveAndLoadInstaller : IInstaller
    {
        private readonly StorageProfile storageProfile;
        public SaveAndLoadInstaller(StorageProfile storageProfile) => this.storageProfile = storageProfile;
        public void Install(IContainerBuilder builder)
        {
            // Storage Core
            builder.Register<SlotService>(Lifetime.Singleton)
                .As<ISlotService>();

            builder.Register<FileBlobStore>(Lifetime.Singleton)
                .As<ILocalBlobStore>();

#if UNITY_ANDROID && USE_GPGS
            builder.Register<GpgsBlobStore>(Lifetime.Singleton)
                .As<ICloudBlobStore>();
#else
            builder.Register<NullCloudBlobStore>(Lifetime.Singleton)
                .As<ICloudBlobStore>();
#endif

            builder.Register<IJsonSerializer>(r
                => new NewtonsoftJsonSerializer(), Lifetime.Singleton);
            
            builder.RegisterInstance(storageProfile);

            builder.Register<StorageService>(Lifetime.Singleton)
                .As<IStorageService>()
                .WithParameter(storageProfile);

            // builder.Register<SaveCoordinator>(Lifetime.Singleton)
            //     .As<ISaveCoordinator>();
            
            
            //builder.RegisterEntryPoint<AutoSaveOnAppEvents>();
        }
    }
}