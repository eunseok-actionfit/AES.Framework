using AES.Tools.Core;
using AES.Tools.Impl;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer.Installer.App
{
    public class SaveAndLoadInstaller : IInstaller
    {
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

            // builder.RegisterInstance(new JsonSerializerSettings
            //     {
            //         Formatting = Formatting.None,
            //         MissingMemberHandling = MissingMemberHandling.Ignore,
            //         NullValueHandling = NullValueHandling.Include
            //     })
            //     .AsSelf();
            //
            // builder.Register<NewtonsoftJsonSerializer>(Lifetime.Singleton).As<IJsonSerializer>();
            
        }
    }
}


