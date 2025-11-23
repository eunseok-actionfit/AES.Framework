using UnityEditor;


#if UNITY_EDITOR
namespace AES.Tools.Editor
{
    public static class BindingDebugSettings
    {
        const string MenuPath = "AES/DataBinding/Binding Debug";

        static bool _enabled;

        public static bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                EditorPrefs.SetBool("AES.BindingDebug.Enabled", _enabled);
                Menu.SetChecked(MenuPath, _enabled);
            }
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            _enabled = EditorPrefs.GetBool("AES.BindingDebug.Enabled", false);
            Menu.SetChecked(MenuPath, _enabled);
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            Enabled = !Enabled;
        }
    }
}
#endif