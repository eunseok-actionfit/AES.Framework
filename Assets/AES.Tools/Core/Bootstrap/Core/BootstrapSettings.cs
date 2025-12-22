using System;
using System.Linq;
using AES.Tools.VContainer.Bootstrap.Framework;
using UnityEngine;
using VContainer.Unity;


namespace AES.Tools.VContainer.Bootstrap
{
    public enum AppOpenLoadingMode { None, Prefab, }

    [CreateAssetMenu(menuName = "Game/Bootstrap Settings", fileName = "BootstrapSettings")]
    public sealed class BootstrapSettings : ScriptableObject
    {
        public static BootstrapSettings Instance { get; private set; }

        [Header("Root Scope Prefab (AppLifetimeScope Prefab)")]
        [SerializeField] private LifetimeScope appLifetimeScopePrefab;

        [Header("Bootstrap Graph/Profile (for InitializeAllAsync)")]
        [SerializeField] private BootstrapGraph graph;
        [SerializeField] private string profile = "Dev";

        [Header("Auto Create Root Scope")]
        [SerializeField] private bool autoCreateRootScope = true;

        [Header("Remove (Clone) postfix")]
        [SerializeField] private bool removeClonePostfix = true;

        [Header("First Scene Key (SceneCatalog key)")]
        [SerializeField] private bool enableFirstSceneKey = true;
        [AesEnableIf("enableFirstSceneKey")]
        [SerializeField] private string firstSceneKey = "Lobby";

        [Header("App Open Loading UI")]
        [AesEnumToggleButtons]
        [SerializeField] private AppOpenLoadingMode appOpenLoadingMode = AppOpenLoadingMode.Prefab;
        [AesShowIf("appOpenLoadingMode == AppOpenLoadingMode.Prefab")]
        [SerializeField] private GameObject appOpenLoadingPrefab;

        public LifetimeScope AppLifetimeScopePrefab => appLifetimeScopePrefab;
        public BootstrapGraph Graph => graph;
        public string Profile => profile;

        public bool AutoCreateRootScope => autoCreateRootScope;
        public bool RemoveClonePostfix => removeClonePostfix;

        public string FirstSceneKey => enableFirstSceneKey ? firstSceneKey : string.Empty;
        public AppOpenLoadingMode AppOpenLoading => appOpenLoadingMode;
        public GameObject AppOpenLoadingPrefab => appOpenLoadingPrefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void OnEnable() => Instance = this;

#if UNITY_EDITOR
        public static BootstrapSettings TryLoadFromPreloadedAssets()
        {
            var preloadAsset = UnityEditor.PlayerSettings
                .GetPreloadedAssets()
                .FirstOrDefault(x => x is BootstrapSettings);

            return preloadAsset as BootstrapSettings;
        }
#endif
    }
}