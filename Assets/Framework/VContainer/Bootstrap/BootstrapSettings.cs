using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer
{
    [CreateAssetMenu(menuName = "Game/Bootstrap Settings", fileName = "BootstrapSettings")]
    public sealed class BootstrapSettings : ScriptableObject
    {
        public static BootstrapSettings Instance { get; private set; }

        private static LifetimeScope _rootLifetimeScopeInstance;

        [Header("Root LifetimeScope 프리팹")]
        [SerializeField]
        private LifetimeScope rootLifetimeScope;

        [Header("시작 시 자동 실행")]
        [SerializeField]
        private bool autoRunRootLifetimeScope = true;

        [Header("이름에서 (Clone) 제거")]
        [SerializeField]
        private bool removeClonePostfix = true;

        [Header("선택: 첫 씬 이름 (비워두면 그대로 현재 씬 사용)")]
        [SerializeField]
        private string firstSceneName;
        

        #region Static reset

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            _rootLifetimeScopeInstance = null;
        }

        #endregion

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/Game/Bootstrap Settings (Preloaded)")]
        public static void CreateAssetAsPreloaded()
        {
            var path = UnityEditor.EditorUtility.SaveFilePanelInProject(
                "Save BootstrapSettings",
                "BootstrapSettings",
                "asset",
                string.Empty);

            if (string.IsNullOrEmpty(path))
                return;

            var newSettings = CreateInstance<BootstrapSettings>();
            UnityEditor.AssetDatabase.CreateAsset(newSettings, path);

            var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets().ToList();
            preloadedAssets.RemoveAll(x => x is BootstrapSettings);
            preloadedAssets.Add(newSettings);
            UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());

            UnityEditor.Selection.activeObject = newSettings;
        }

        private static BootstrapSettings LoadFromPreloadedAssets()
        {
            var preloadAsset = UnityEditor.PlayerSettings
                .GetPreloadedAssets()
                .FirstOrDefault(x => x is BootstrapSettings);
            return preloadAsset as BootstrapSettings;
        }
#endif

        #region Bootstrap entry

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            BootstrapSettings settings;

#if UNITY_EDITOR
            settings = LoadFromPreloadedAssets();
#endif
            if (settings == null)
            {
                settings = Resources.Load<BootstrapSettings>("BootstrapSettings");
            }

            if (settings == null)
            {
                Debug.LogWarning("[BootstrapSettings] 설정 에셋을 찾지 못했습니다.");
                return;
            }

            Instance = settings;

            // 1. Root LifetimeScope
            if (Instance.autoRunRootLifetimeScope)
            {
                Instance.GetOrCreateRootLifetimeScopeInstance();
            }

            // 2. 모듈 초기화 (세이브/로거/SDK 등)
            Instance.InitializeModules();

            // 3. 첫 씬 로드
            if (!string.IsNullOrEmpty(Instance.firstSceneName))
            {
                var active = SceneManager.GetActiveScene();
                if (active.name != Instance.firstSceneName)
                {
                    SceneManager.LoadScene(Instance.firstSceneName, LoadSceneMode.Single);
                }
            }
        }

        #endregion

        #region Root LifetimeScope 관리

        public LifetimeScope GetOrCreateRootLifetimeScopeInstance()
        {
            if (_rootLifetimeScopeInstance != null &&
                _rootLifetimeScopeInstance.Container != null)
            {
                return _rootLifetimeScopeInstance;
            }

            if (rootLifetimeScope == null)
            {
                Debug.LogError("[BootstrapSettings] rootLifetimeScope 프리팹이 설정되지 않았습니다.");
                return null;
            }

            var activeBefore = rootLifetimeScope.gameObject.activeSelf;
            rootLifetimeScope.gameObject.SetActive(false);

            _rootLifetimeScopeInstance = Instantiate(rootLifetimeScope);
            SetName(_rootLifetimeScopeInstance, rootLifetimeScope);
            DontDestroyOnLoad(_rootLifetimeScopeInstance);
            _rootLifetimeScopeInstance.gameObject.SetActive(true);

            rootLifetimeScope.gameObject.SetActive(activeBefore);

            return _rootLifetimeScopeInstance;
        }

        public bool IsRootLifetimeScopeInstance(LifetimeScope lifetimeScope) =>
            rootLifetimeScope == lifetimeScope || _rootLifetimeScopeInstance == lifetimeScope;

        private void SetName(Object instance, Object prefab)
        {
            if (removeClonePostfix)
                instance.name = prefab.name;
        }

        #endregion

        #region Modules

        private void InitializeModules()
        {
            var root = _rootLifetimeScopeInstance; // null일 수도 있음

            var modules = root.Container.Resolve<AppConfig>().modules;
            if (modules == null)
                return;
            
            
            for (int i = 0; i < modules.Length; i++)
            {
                var m = modules[i];
                if (m == null) continue;

                try
                {
                    m.Initialize(root);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        #endregion
    }
}
