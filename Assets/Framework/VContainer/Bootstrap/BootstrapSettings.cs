using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace MyGame
{
    [CreateAssetMenu(menuName = "MyGame/Bootstrap Settings", fileName = "BootstrapSettings")]
    public sealed class BootstrapSettings : ScriptableObject
    {
        public static BootstrapSettings Instance { get; private set; }

        static LifetimeScope rootLifetimeScopeInstance;

        [Header("Root LifetimeScope 프리팹")]
        [SerializeField] LifetimeScope rootLifetimeScope;

        [Header("시작 시 자동 실행")]
        [SerializeField] bool autoRunRootLifetimeScope = true;

        [Header("이름에서 (Clone) 제거")]
        [SerializeField] bool removeClonePostfix = true;

        [Header("선택: 첫 씬 이름 (비워두면 그대로 현재 씬 사용)")]
        [SerializeField] string firstSceneName;

        [Header("부트스트랩 모듈 (세이브/로거/SDK 등)")]
        [SerializeField] BootstrapModule[] modules;

        #region Static reset

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            rootLifetimeScopeInstance = null;
        }

        #endregion

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/MyGame/Bootstrap Settings (Preloaded)")]
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

        static BootstrapSettings LoadFromPreloadedAssets()
        {
            var preloadAsset = UnityEditor.PlayerSettings
                .GetPreloadedAssets()
                .FirstOrDefault(x => x is BootstrapSettings);
            return preloadAsset as BootstrapSettings;
        }
#endif

        #region Bootstrap entry

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
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
            if (rootLifetimeScopeInstance != null &&
                rootLifetimeScopeInstance.Container != null)
            {
                return rootLifetimeScopeInstance;
            }

            if (rootLifetimeScope == null)
            {
                Debug.LogError("[BootstrapSettings] rootLifetimeScope 프리팹이 설정되지 않았습니다.");
                return null;
            }

            var activeBefore = rootLifetimeScope.gameObject.activeSelf;
            rootLifetimeScope.gameObject.SetActive(false);

            rootLifetimeScopeInstance = Instantiate(rootLifetimeScope);
            SetName(rootLifetimeScopeInstance, rootLifetimeScope);
            DontDestroyOnLoad(rootLifetimeScopeInstance);
            rootLifetimeScopeInstance.gameObject.SetActive(true);

            rootLifetimeScope.gameObject.SetActive(activeBefore);

            return rootLifetimeScopeInstance;
        }

        public bool IsRootLifetimeScopeInstance(LifetimeScope lifetimeScope) =>
            rootLifetimeScope == lifetimeScope || rootLifetimeScopeInstance == lifetimeScope;

        void SetName(Object instance, Object prefab)
        {
            if (removeClonePostfix)
                instance.name = prefab.name;
        }

        #endregion

        #region Modules

        void InitializeModules()
        {
            if (modules == null)
                return;

            var root = rootLifetimeScopeInstance; // null일 수도 있음
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
