using System;
using System.Collections.Generic;
using UnityEngine;

namespace AES.Tools.VContainer.Bootstrap.Framework
{
    [CreateAssetMenu(menuName = "Game/Bootstrap/Bootstrap Graph", fileName = "BootstrapGraph")]
    public sealed class BootstrapGraph : ScriptableObject
    {
        [SerializeField] private string defaultProfile = "Dev";
        [SerializeField] private FeatureProfile[] profiles;

        public string DefaultProfile => string.IsNullOrWhiteSpace(defaultProfile) ? "Dev" : defaultProfile;
        public IReadOnlyList<FeatureProfile> Profiles => profiles;

        public bool TryGetProfile(string profileName, out FeatureProfile profile)
        {
            profile = null;
            if (profiles == null) return false;

            foreach (var p in profiles)
            {
                if (p != null && string.Equals(p.ProfileName, profileName, StringComparison.OrdinalIgnoreCase))
                {
                    profile = p;
                    return true;
                }
            }
            return false;
        }
    }

    [Serializable]
    public sealed class FeatureProfile
    {
        [SerializeField] private string profileName = "Dev";
        [SerializeField] private FeatureEntry[] features;

        public string ProfileName => string.IsNullOrWhiteSpace(profileName) ? "Dev" : profileName;
        public IReadOnlyList<FeatureEntry> Features => features;
    }

    [Serializable]
    public sealed class FeatureEntry
    {
        [SerializeField] private AppFeatureSO feature;   
        [SerializeField] private bool enabled = true;
        [SerializeField] private OverridePair[] overrides;

        public AppFeatureSO Feature => feature;
        public bool Enabled => enabled;
        public IReadOnlyList<OverridePair> Overrides => overrides;

        public string FeatureId => feature ? feature.Id : "<null>";
    }

    [Serializable]
    public sealed class OverridePair
    {
        public string key;
        public UnityEngine.Object value;
    }
}
