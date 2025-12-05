#if UNITY_EDITOR
using UnityEditor;

namespace AES.Tools.Editor
{
    public static class UIBasePrefabPrefs
    {
        private const string KEY_BUTTON_PATH = "AES.UIBasePrefab.ButtonPath";
        private const string KEY_PANEL_PATH  = "AES.UIBasePrefab.PanelPath";

        public static string ButtonPath
        {
            get => EditorPrefs.GetString(KEY_BUTTON_PATH, "");
            set => EditorPrefs.SetString(KEY_BUTTON_PATH, value ?? "");
        }

        public static string PanelPath
        {
            get => EditorPrefs.GetString(KEY_PANEL_PATH, "");
            set => EditorPrefs.SetString(KEY_PANEL_PATH, value ?? "");
        }
    }
}
#endif