using UnityEditor;
using UnityEngine;

public class MenuCrashTest : EditorWindow
{
    [MenuItem("Test/GenericMenuCrash")]
    static void Open()
    {
        GetWindow<MenuCrashTest>();
    }

    void OnGUI()
    {
        if (GUILayout.Button("Open menu"))
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Test Item"), false, () => {});
            menu.ShowAsContext();
        }
    }
}