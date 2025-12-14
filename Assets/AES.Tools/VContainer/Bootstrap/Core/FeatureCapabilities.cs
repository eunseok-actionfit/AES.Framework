using System;
using System.Collections.Generic;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    public interface IFeatureCapability { }

    public interface IProvideCapabilities
    {
        void Provide(IDictionary<Type, IFeatureCapability> caps);
    }

    public static class FeatureCapabilityExtensions
    {
        public static bool TryGetCapability<T>(this IReadOnlyDictionary<Type, IFeatureCapability> caps, out T cap)
            where T : class, IFeatureCapability
        {
            if (caps != null && caps.TryGetValue(typeof(T), out var v))
            {
                cap = (T)v;
                return true;
            }

            cap = null;
            return false;
        }
    }
    

    public interface IIapCatalogProvider : IFeatureCapability
    {
        IReadOnlyList<IAP.IapStoreCatalogEntry> GetCatalog();
    }

    public interface IAnalyticsBootstrap : IFeatureCapability { void Initialize(); }
    public interface IRemoteConfigBootstrap : IFeatureCapability { Cysharp.Threading.Tasks.UniTask InitializeAsync(); }
}