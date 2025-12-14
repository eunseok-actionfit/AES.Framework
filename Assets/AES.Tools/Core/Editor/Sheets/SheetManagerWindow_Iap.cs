#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AES.IAP.Editor.Sheets
{
    public sealed class SheetManagerWindow_Iap : EditorWindow
    {
        [MenuItem("AES/IAP/Sheet Manager")]
        private static void Open() => GetWindow<SheetManagerWindow_Iap>("IAP Sheet").Show();

        private SheetAssetProfile_Iap profile;

        private void OnEnable()
        {
            var guid = AssetDatabase.FindAssets("t:SheetAssetProfile_Iap").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
                profile = AssetDatabase.LoadAssetAtPath<SheetAssetProfile_Iap>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void OnGUI()
        {
            profile = (SheetAssetProfile_Iap)EditorGUILayout.ObjectField("Profile", profile, typeof(SheetAssetProfile_Iap), false);
            if (profile == null) return;

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Generate All", GUILayout.Height(28)))
                SheetDataProcessor_IapJson.GenerateAll(profile);

            EditorGUILayout.Space(12);
            if (profile.sheets == null) return;

            foreach (var s in profile.sheets)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{s.name} ({s.mode}, gid={s.gid})");
                    if (GUILayout.Button("Generate", GUILayout.Width(90)))
                        SheetDataProcessor_IapJson.GenerateOne(profile, s);
                }
            }
        }
    }
}
#endif