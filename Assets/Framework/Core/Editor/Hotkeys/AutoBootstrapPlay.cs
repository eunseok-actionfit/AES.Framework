#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _Project.Scripts.Core.Editor
{
    /// <summary>
    /// AutoBootstrapPlay 관련 에디터 환경설정 저장소
    /// </summary>
    public static class AutoBootstrapPrefs
    {
        public const string KeyEnabled   = "AutoBootstrapPlay.Enabled";
        public const string KeyScenePath = "AutoBootstrapPlay.ScenePath";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(KeyEnabled, false);
            set => EditorPrefs.SetBool(KeyEnabled, value);
        }

        public static string ScenePath
        {
            get => EditorPrefs.GetString(KeyScenePath, "");
            set => EditorPrefs.SetString(KeyScenePath, value ?? "");
        }
    }

    /// <summary>
    /// 에디터에서 Play 버튼을 누를 때
    /// 특정 Bootstrap 씬부터 자동 실행하도록 설정하는 기능
    /// </summary>
    [InitializeOnLoad]
    public static class AutoBootstrapPlay
    {
        private const string MENU_PATH = "Tools/Play From Bootstrapper (Auto)";

        // 초기화 시 에디터 이벤트 등록
        static AutoBootstrapPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += SyncMenu;
        }

        #region MenuItem Methods
        [MenuItem(MENU_PATH)]
        private static void Toggle()
        {
            AutoBootstrapPrefs.Enabled = !AutoBootstrapPrefs.Enabled;
            SyncMenu();
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MENU_PATH, AutoBootstrapPrefs.Enabled);
            return true;
        }

        private static void SyncMenu()
            => Menu.SetChecked(MENU_PATH, AutoBootstrapPrefs.Enabled);
        #endregion

        /// <summary>
        /// Play 모드로 진입할 때 부트스트랩 씬 지정
        /// </summary>
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            if (!AutoBootstrapPrefs.Enabled)
            {
                // 설정 꺼짐 → 현재 열린 씬에서 바로 실행
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var path = AutoBootstrapPrefs.ScenePath;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[AutoBootstrapPlay] Bootstrap scene is not set. Open Tools > Bootstrapper Settings.");
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset == null)
            {
                Debug.LogError($"[AutoBootstrapPlay] Scene asset not found: {path}");
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            EditorSceneManager.playModeStartScene = sceneAsset;
            Debug.Log($"[AutoBootstrapPlay] Play will start from: {path}");
        }
    }
}
#endif
