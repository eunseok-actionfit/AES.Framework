#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "tsv")]
public sealed class TsvImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var text = File.ReadAllText(ctx.assetPath, System.Text.Encoding.UTF8);
        var ta = new TextAsset(text);

        ctx.AddObjectToAsset("Text", ta);
        ctx.SetMainObject(ta); 
    }
}
#endif