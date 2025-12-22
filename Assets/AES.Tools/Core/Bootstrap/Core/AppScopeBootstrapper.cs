using UnityEngine;
using VContainer.Unity;

namespace AES.Tools.VContainer.Bootstrap
{
    public static class AppScopeBootstrapper
    {
        private static LifetimeScope _rootInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_rootInstance != null) return;

            BootstrapSettings settings = null;

#if UNITY_EDITOR
            settings = BootstrapSettings.TryLoadFromPreloadedAssets();
#endif
            if (settings == null)
                settings = Resources.Load<BootstrapSettings>("BootstrapSettings");

            if (settings == null)
            {
                Debug.LogWarning("[AppScopeBootstrapper] BootstrapSettings not found (Resources/Preloaded).");
                return;
            }

            if (!settings.AutoCreateRootScope)
                return;

            if (settings.AppLifetimeScopePrefab == null)
            {
                Debug.LogError("[AppScopeBootstrapper] AppLifetimeScopePrefab is null in BootstrapSettings.");
                return;
            }

            var prefab = settings.AppLifetimeScopePrefab;
            var activeBefore = prefab.gameObject.activeSelf;
            prefab.gameObject.SetActive(false);

            _rootInstance = Object.Instantiate(prefab);
            if (settings.RemoveClonePostfix)
                _rootInstance.name = prefab.name;

            Object.DontDestroyOnLoad(_rootInstance);
            _rootInstance.gameObject.SetActive(true);

            prefab.gameObject.SetActive(activeBefore);
        }
    }
}