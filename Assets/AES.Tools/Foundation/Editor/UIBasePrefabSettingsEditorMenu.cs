#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Editor
{
    public class UIBasePrefabSettingsWindow : EditorWindow
    {
        private Object _buttonPrefab;
        private Object _panelPrefab;

        [MenuItem("AES/UI Base Prefab Settings")]
        public static void Open()
            => GetWindow<UIBasePrefabSettingsWindow>("UI Base Prefabs");

        private void OnEnable()
        {
            _buttonPrefab = string.IsNullOrEmpty(UIBasePrefabPrefs.ButtonPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Object>(UIBasePrefabPrefs.ButtonPath);

            _panelPrefab = string.IsNullOrEmpty(UIBasePrefabPrefs.PanelPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Object>(UIBasePrefabPrefs.PanelPath);
        }

        private void OnGUI()
        {
            GUILayout.Label("Base UI Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "여기서 지정한 Base Prefab을 GameObject > UI 메뉴에서 사용합니다.",
                MessageType.Info
            );

            EditorGUI.BeginChangeCheck();
            _buttonPrefab = EditorGUILayout.ObjectField(
                "Button Base Prefab",
                _buttonPrefab,
                typeof(GameObject),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                var path = _buttonPrefab ? AssetDatabase.GetAssetPath(_buttonPrefab) : "";
                UIBasePrefabPrefs.ButtonPath = path;
            }

            EditorGUI.BeginChangeCheck();
            _panelPrefab = EditorGUILayout.ObjectField(
                "Panel Base Prefab",
                _panelPrefab,
                typeof(GameObject),
                false
            );
            if (EditorGUI.EndChangeCheck())
            {
                var path = _panelPrefab ? AssetDatabase.GetAssetPath(_panelPrefab) : "";
                UIBasePrefabPrefs.PanelPath = path;
            }

            GUILayout.Space(4);
            EditorGUILayout.LabelField("Button Path", UIBasePrefabPrefs.ButtonPath);
            EditorGUILayout.LabelField("Panel Path",  UIBasePrefabPrefs.PanelPath);

            GUILayout.Space(6);
            if (GUILayout.Button("Clear All", GUILayout.Height(22)))
            {
                _buttonPrefab = null;
                _panelPrefab  = null;
                UIBasePrefabPrefs.ButtonPath = "";
                UIBasePrefabPrefs.PanelPath  = "";
            }
        }
    }
}
#endif
