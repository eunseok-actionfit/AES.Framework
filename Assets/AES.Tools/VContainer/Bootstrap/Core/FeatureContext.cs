using System;
using System.Collections.Generic;
using UnityEngine;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    [Serializable]
    public readonly struct FeatureContext
    {
        public readonly string Profile;
        public readonly RuntimePlatform Platform;
        public readonly bool IsEditor;

        public readonly IReadOnlyDictionary<Type, IFeatureCapability> Capabilities;
        public readonly IReadOnlyDictionary<string, UnityEngine.Object> Overrides;

        public FeatureContext(
            string profile,
            RuntimePlatform platform,
            bool isEditor,
            IReadOnlyDictionary<string, UnityEngine.Object> overrides,
            IReadOnlyDictionary<Type, IFeatureCapability> capabilities)
        {
            Profile = profile;
            Platform = platform;
            IsEditor = isEditor;
            Overrides = overrides;
            Capabilities = capabilities;
        }

        public bool TryGetOverride<T>(string key, out T value) where T : UnityEngine.Object
        {
            value = null;
            if (Overrides == null) return false;
            if (!Overrides.TryGetValue(key, out var obj)) return false;
            value = obj as T;
            return value != null;
        }

        public bool TryGetCapability<T>(out T cap) where T : class, IFeatureCapability
        {
            cap = null;
            if (Capabilities == null) return false;
            if (!Capabilities.TryGetValue(typeof(T), out var v)) return false;
            cap = v as T;
            return cap != null;
        }
    }
}