#if UNITY_EDITOR
using System.Linq;
using AES.Tools.VContainer.Bootstrap;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.VContainer
{
    // UnityEditor 전용 메뉴 유틸
    static class BootstrapSettingsMenu
    {
        const string DefaultResourcesPath = "BootstrapSettings";

        [MenuItem("AES/Game/Open Bootstrap Settings")]
        static void OpenBootstrapSettings()
        {
            var settings = FindBootstrapSettingsAsset();

            // 못 찾았으면 생성 여부 물어보고, 원하면 새로 생성
            if (settings == null)
            {
                if (!EditorUtility.DisplayDialog(
                        "BootstrapSettings 없음",
                        "BootstrapSettings 에셋을 찾지 못했습니다.\n새로 생성할까요?",
                        "생성",
                        "취소"))
                {
                    return;
                }

                settings = CreateBootstrapSettingsAsset();
                if (settings == null)
                    return;
            }

            // Project 창에서 해당 에셋 선택
            Selection.activeObject = settings;
            EditorUtility.FocusProjectWindow();
        }

        static BootstrapSettings FindBootstrapSettingsAsset()
        {
            // 1. Preloaded Assets에서 찾아보기
            var preload = PlayerSettings
                .GetPreloadedAssets()
                .FirstOrDefault(x => x is BootstrapSettings) as BootstrapSettings;

            if (preload != null)
                return preload;

            // 2. Resources 폴더에서 기본 경로로 로드
            var fromResources = Resources.Load<BootstrapSettings>(DefaultResourcesPath);
            if (fromResources != null)
                return fromResources;

            // 3. 프로젝트 전체에서 타입으로 검색
            var guids = AssetDatabase.FindAssets("t:BootstrapSettings");
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<BootstrapSettings>(path);
            }

            return null;
        }

        static BootstrapSettings CreateBootstrapSettingsAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save BootstrapSettings",
                "BootstrapSettings",
                "asset",
                string.Empty);

            if (string.IsNullOrEmpty(path))
                return null;

            var newSettings = ScriptableObject.CreateInstance<BootstrapSettings>();
            AssetDatabase.CreateAsset(newSettings, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newSettings;
        }
    }
}
#endif
