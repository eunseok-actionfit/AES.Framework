using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AES.Tools.VContainer.Bootstrap.Framework.Features
{
    [CreateAssetMenu(menuName = "Game/Bootstrap/Features/Storage Feature", fileName = "StorageFeature")]
    public sealed class StorageFeature : AppFeatureSO
    {
        [SerializeField] private StorageProfile storageProfile;

        public override void Install(IContainerBuilder b, in FeatureContext ctx)
        {
            // Storage Core
            b.Register<SlotService>(Lifetime.Singleton).As<ISlotService>();
            b.Register<FileBlobStore>(Lifetime.Singleton).As<ILocalBlobStore>();

#if UNITY_ANDROID && USE_GPGS
            b.Register<GpgsBlobStore>(Lifetime.Singleton).As<ICloudBlobStore>();
#else
            b.Register<NullCloudBlobStore>(Lifetime.Singleton).As<ICloudBlobStore>();
#endif

            b.Register<IJsonSerializer>(_ => new NewtonsoftJsonSerializer(), Lifetime.Singleton);

            if (storageProfile != null)
                b.RegisterInstance(storageProfile);

            b.Register<StorageService>(Lifetime.Singleton)
                .As<IStorageService>()
                .WithParameter(storageProfile);

        }

        public override UniTask Initialize(LifetimeScope rootScope, FeatureContext ctx)
            => UniTask.CompletedTask;
    }
}