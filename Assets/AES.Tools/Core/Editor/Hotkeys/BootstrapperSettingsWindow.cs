#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    /// <summary>
    /// Bootstrapper 씬 설정 전용 에디터 윈도우.
    /// - AutoBootstrapPlay와 연동되어, 플레이 시 시작 씬을 지정 가능.
    /// - 메뉴 경로: Tools > Bootstrapper Settings
    /// </summary>
    public class BootstrapperSettingsWindow : EditorWindow
    {
        private Object _sceneAsset;

        [MenuItem("AES/Bootstrapper Settings")]
        public static void Open()
            => GetWindow<BootstrapperSettingsWindow>("Bootstrapper Settings");

        private void OnEnable()
        {
            var path = AutoBootstrapPrefs.ScenePath;
            _sceneAsset = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<Object>(path);
        }

        private void OnGUI()
        {
            GUILayout.Label("Bootstrap Scene", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play 버튼을 누르면 이 씬부터 시작합니다.\n(메뉴: AES > Play From Bootstrapper (Auto))",
                MessageType.Info
            );

            // 씬 에셋 선택 필드
            EditorGUI.BeginChangeCheck();
            _sceneAsset = EditorGUILayout.ObjectField(
                "Scene Asset",
                _sceneAsset,
                typeof(SceneAsset),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                var path = _sceneAsset ? AssetDatabase.GetAssetPath(_sceneAsset) : "";
                AutoBootstrapPrefs.ScenePath = path;
            }

            GUILayout.Space(6);

            // 토글 & 클리어 버튼
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        AutoBootstrapPrefs.Enabled ? "Auto Play: ON" : "Auto Play: OFF",
                        GUILayout.Height(24)))
                {
                    AutoBootstrapPrefs.Enabled = !AutoBootstrapPrefs.Enabled;
                }

                if (GUILayout.Button("Clear", GUILayout.Height(24)))
                {
                    _sceneAsset = null;
                    AutoBootstrapPrefs.ScenePath = "";
                }
            }

            GUILayout.Space(4);

            // 저장된 경로 표시
            EditorGUILayout.LabelField("Saved Path", AutoBootstrapPrefs.ScenePath);
        }
    }
}
#endif
