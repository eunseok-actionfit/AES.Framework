using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace AES.Tools.VContainer.Bootstrap.Framework.Editor
{
    public enum FeatureCategory { InstallOnly, InitOnly, Both }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class FeatureCategoryAttribute : Attribute
    {
        public readonly FeatureCategory Category;
        public FeatureCategoryAttribute(FeatureCategory category) => Category = category;
    }

    public static class BootstrapGraphQuickAdd
    {
        private const string DefaultFolder = "Assets/_Project/Bootstrap/Features";

        public static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(DefaultFolder)) return;

            var parts = DefaultFolder.Split('/');
            string cur = parts[0]; // Assets
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        public static FeatureCategory GetCategory(Type t)
        {
            var attr = (FeatureCategoryAttribute)Attribute.GetCustomAttribute(t, typeof(FeatureCategoryAttribute));
            if (attr != null) return attr.Category;

    
            // Attribute 없으면 override 여부로 자동 분류
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var mInstall = t.GetMethod(nameof(AppFeatureSO.Install), flags);
            var mInit = t.GetMethod(nameof(AppFeatureSO.Initialize), flags);

            bool overridesInstall = mInstall != null && mInstall.DeclaringType == t;
            bool overridesInit = mInit != null && mInit.DeclaringType == t;

            if (overridesInstall && overridesInit) return FeatureCategory.Both;
            if (overridesInstall) return FeatureCategory.InstallOnly;
            if (overridesInit) return FeatureCategory.InitOnly;

            return FeatureCategory.Both;
        }


        public static Type[] FindAllFeatureTypes()
        {
            return TypeCache.GetTypesDerivedFrom<AppFeatureSO>()
                .Where(t => !t.IsAbstract)
                .Where(t => !Attribute.IsDefined(t, typeof(HideInFeatureMenuAttribute)))
                .OrderBy(t => t.Name)
                .ToArray();
        }

        public static AppFeatureSO FindExistingById(string featureId)
        {
            EnsureFolder();

            // 폴더 내 ScriptableObject 전체에서 AppFeatureSO만 로드
            var guids = AssetDatabase.FindAssets("t:AppFeatureSO", new[] { DefaultFolder });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var obj = AssetDatabase.LoadAssetAtPath<AppFeatureSO>(path);
                if (obj && string.Equals(obj.Id, featureId, StringComparison.Ordinal))
                    return obj;
            }
            return null;
        }

        public static AppFeatureSO CreateFeatureAsset(Type featureType, string assetName)
        {
            EnsureFolder();
            var path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultFolder}/{assetName}.asset");
            var asset = (AppFeatureSO)ScriptableObject.CreateInstance(featureType);
            AssetDatabase.CreateAsset(asset, path);

           
            var so = new SerializedObject(asset);
            var idProp = so.FindProperty("id");
            if (idProp != null) idProp.stringValue = assetName;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }


        public static AppFeatureSO FindOrCreate(Type featureType)
        {
            // 베이스에 id가 있으니: 임시 인스턴스 만들어 id 확보 → 같은 id 에셋 재사용
            var temp = (AppFeatureSO)ScriptableObject.CreateInstance(featureType);
            string id;
            try { id = string.IsNullOrWhiteSpace(temp.Id) ? featureType.Name : temp.Id; }
            finally { UnityEngine.Object.DestroyImmediate(temp); }

            var existing = FindExistingById(id);
            if (existing) return existing;

            // 파일명은 Id로 고정 (중복 생성 방지)
            return CreateFeatureAsset(featureType, assetName: id);
        }

        public static void AddToProfile(BootstrapGraph graph, int profileIndex, AppFeatureSO feature, bool enabled = true)
        {
            if (!graph || !feature) return;

            var so = new SerializedObject(graph);
            var profilesProp = so.FindProperty("profiles");
            if (profilesProp.arraySize == 0) return;

            profileIndex = Mathf.Clamp(profileIndex, 0, profilesProp.arraySize - 1);
            var pProp = profilesProp.GetArrayElementAtIndex(profileIndex);
            var featuresProp = pProp.FindPropertyRelative("features");

            // 중복 엔트리 방지(같은 Id)
            for (int i = 0; i < featuresProp.arraySize; i++)
            {
                var entryProp = featuresProp.GetArrayElementAtIndex(i);
                var fProp = entryProp.FindPropertyRelative("feature");
                var existing = fProp.objectReferenceValue as AppFeatureSO;
                if (existing && string.Equals(existing.Id, feature.Id, StringComparison.Ordinal))
                {
                    EditorGUIUtility.PingObject(existing);
                    Selection.activeObject = existing;
                    return;
                }
            }

            int idx = featuresProp.arraySize;
            featuresProp.InsertArrayElementAtIndex(idx);
            var added = featuresProp.GetArrayElementAtIndex(idx);

            added.FindPropertyRelative("feature").objectReferenceValue = feature;
            added.FindPropertyRelative("enabled").boolValue = enabled;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();

            EditorGUIUtility.PingObject(feature);
            Selection.activeObject = feature;
        }

        public static void ShowCreateAddMenu(BootstrapGraph graph, int profileIndex)
        {
            var types = FindAllFeatureTypes();
            var menu = new GenericMenu();

            void AddGroup(FeatureCategory cat, string title)
            {
                var group = types.Where(t => GetCategory(t) == cat).ToArray();
                if (group.Length == 0)
                {
                    menu.AddDisabledItem(new GUIContent($"{title}/(none)"));
                    return;
                }

                foreach (var t in group)
                {
                    // 표시 라벨은 Type명, 실제 생성/재사용은 Id 기준
                    menu.AddItem(new GUIContent($"{title}/{t.Name}"), false, () =>
                    {
                        var asset = FindOrCreate(t);
                        AddToProfile(graph, profileIndex, asset, enabled: true);
                    });
                }
            }

            AddGroup(FeatureCategory.InstallOnly, "Install-only");
            AddGroup(FeatureCategory.InitOnly, "Init-only");
            AddGroup(FeatureCategory.Both, "Both");

            menu.ShowAsContext();
        }
    }
}
