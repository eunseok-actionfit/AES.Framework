using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;
using AES.Tools.VContainer.Bootstrap.Framework;
using Cysharp.Threading.Tasks;


namespace AES.Tools.VContainer.Bootstrap
{
    [CreateAssetMenu(menuName = "Game/Bootstrap Settings", fileName = "BootstrapSettings")]
    public sealed class BootstrapSettings : ScriptableObject
    {
        public static BootstrapSettings Instance { get; private set; }
        private static LifetimeScope _rootLifetimeScopeInstance;

        [Header("Root LifetimeScope 프리팹")]
        [SerializeField] private LifetimeScope rootLifetimeScope;

        [Header("Feature Graph")]
        [SerializeField] private BootstrapGraph graph;

        [SerializeField] private string profile = "Dev";

        [Header("시작 시 자동 실행")]
        [SerializeField] private bool autoRunRootLifetimeScope = true;

        [Header("이름에서 (Clone) 제거")]
        [SerializeField] private bool removeClonePostfix = true;

        [Header("선택: 첫 씬 이름 (비워두면 그대로 현재 씬 사용)")]
        [SerializeField] private string firstSceneName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            _rootLifetimeScopeInstance = null;
        }

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static async void Bootstrap()
        {
            BootstrapSettings settings = null;

#if UNITY_EDITOR
            settings = LoadFromPreloadedAssets();
#endif
            if (settings == null)
                settings = Resources.Load<BootstrapSettings>("BootstrapSettings");

            if (settings == null)
            {
                Debug.LogWarning("[BootstrapSettings] 설정 에셋을 찾지 못했습니다.");
                return;
            }

            Instance = settings;

            if (!Instance.autoRunRootLifetimeScope)
                return;

            Instance.GetOrCreateRootLifetimeScopeInstance();
            
            if (Instance.graph)
            {
                await BootstrapRunner.InitializeAllAsync(
                    Instance.graph,
                    Instance.profile,
                    _rootLifetimeScopeInstance,
                    Application.platform,
#if UNITY_EDITOR
                    true
#else
                    false
#endif
                );
            }
            else
            {
                Debug.LogWarning("[BootstrapSettings] BootstrapGraph가 비어있습니다. Init 단계를 스킵합니다.");
            }
            
            var root = _rootLifetimeScopeInstance;
            if (root == null || root.Container == null) return;

            var sceneFlow = root.Container.Resolve<ISceneFlow>();

            if (!string.IsNullOrEmpty(Instance.firstSceneName))
            {
                var active = SceneManager.GetActiveScene();
                if (active.name != Instance.firstSceneName)
                {
                    await sceneFlow.LoadWithArgsAsync<object>(Instance.firstSceneName, "Home.Load", null);
                }
            }
            await UniTask.NextFrame();
            ADS.MarkAppReadyForAppOpen();
            ADS.TryShowAppOpen("app_launch");
        }

        private LifetimeScope GetOrCreateRootLifetimeScopeInstance()
        {
            if (_rootLifetimeScopeInstance != null &&
                _rootLifetimeScopeInstance.Container != null) return _rootLifetimeScopeInstance;

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
    }
}
