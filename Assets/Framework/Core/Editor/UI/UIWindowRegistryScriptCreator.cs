// Assets/Whatever/Editor/UIWindowRegistryScriptCreator.cs
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;


namespace AES.Tools.Editor
{
    public sealed class UIWindowRegistryScriptCreator : EndNameEditAction
    {
        [MenuItem("Assets/Create/UI/Window Enum+Registry Script", priority = 2000)]
        private static void CreateScripts()
        {
            var icon        = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
            const string defaultName = "NewUIWindow";

            var folder = GetSelectedFolderPath();
            var path   = Path.Combine(folder, defaultName + ".cs").Replace("\\", "/");

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                CreateInstance<UIWindowRegistryScriptCreator>(),
                path,
                icon,
                null);
        }

        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var folder   = Path.GetDirectoryName(pathName)!.Replace("\\", "/");
            var baseName = Path.GetFileNameWithoutExtension(pathName); // 입력한 파일명

            var enumName     = baseName + "Id";
            var registryName = baseName + "Registry";

            var enumPath     = Path.Combine(folder, enumName + ".cs").Replace("\\", "/");
            var registryPath = Path.Combine(folder, registryName + ".cs").Replace("\\", "/");

            File.WriteAllText(enumPath,     GenerateEnumCode(enumName),                 Encoding.UTF8);
            File.WriteAllText(registryPath, GenerateRegistryCode(enumName, registryName), Encoding.UTF8);

            AssetDatabase.Refresh();

            var enumAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(enumPath);
            ProjectWindowUtil.ShowCreatedAsset(enumAsset);
        }

        private static string GetSelectedFolderPath()
        {
            var obj = Selection.activeObject;
            if (obj == null)
                return "Assets";

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                return "Assets";

            if (Directory.Exists(path))
                return path;

            return Path.GetDirectoryName(path) ?? "Assets";
        }

        private static string GenerateEnumCode(string enumName)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"public enum {enumName}");
            sb.AppendLine("{");
            sb.AppendLine("    // add window ids here");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GenerateRegistryCode(string enumName, string registryName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using Core.Systems.UI.Registry;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"[CreateAssetMenu(menuName = \"UI/Registry/{enumName}\")]");
            sb.AppendLine($"public sealed class {registryName} : UIWindowRegistryBase<{enumName}>");
            sb.AppendLine("{");
            sb.AppendLine("    // customize if needed");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
