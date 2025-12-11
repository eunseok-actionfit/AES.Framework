#if UNITY_EDITOR
using AES.Tools.VContainer.Bootstrap;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.VContainer
{
    static class AppConfigMenu
    {
        [MenuItem("AES/Game/Open AppConfig")]
        static void OpenAppConfig()
        {
            var config = FindAppConfigAsset();

            if (config == null)
            {
                if (!EditorUtility.DisplayDialog(
                    "AppConfig 없음",
                    "AppConfig 에셋을 찾지 못했습니다.\n새로 생성할까요?",
                    "생성",
                    "취소"))
                {
                    return;
                }

                config = CreateAppConfigAsset();
                if (config == null)
                    return;
            }

            // 프로젝트 창에서 선택
            Selection.activeObject = config;
            EditorUtility.FocusProjectWindow();
        }

        static AppConfig FindAppConfigAsset()
        {
            var guids = AssetDatabase.FindAssets("t:AppConfig");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AppConfig>(path);
            }

            return null;
        }

        static AppConfig CreateAppConfigAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save AppConfig",
                "AppConfig",
                "asset",
                string.Empty);

            if (string.IsNullOrEmpty(path))
                return null;

            var newConfig = ScriptableObject.CreateInstance<AppConfig>();
            AssetDatabase.CreateAsset(newConfig, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newConfig;
        }
    }
}
#endif