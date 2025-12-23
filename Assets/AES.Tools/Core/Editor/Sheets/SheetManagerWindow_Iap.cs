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
                    string src = (s.tsv != null) ? $"TSV={s.tsv.name}" : $"gid={s.gid}";
                    EditorGUILayout.LabelField($"{s.name} ({s.mode}, {src})");
                    if (GUILayout.Button("Generate", GUILayout.Width(90)))
                    {
                        // GenerateOne을 직접 호출해도 되지만, GenerateAll에서 lookup을 만드는 편이 더 안전.
                        // 여기서는 간단히 전체 생성 권장. 필요하면 개별 생성에서도 lookup 만들도록 확장 가능.
                        SheetDataProcessor_IapJson.GenerateAll(profile);
                    }
                }
            }
        }
    }
}
#endif