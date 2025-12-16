#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AES.Tools.VContainer.Bootstrap.Framework.Features;

[CustomEditor(typeof(AdsFeature))]
public class AdsFeatureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Import Test Devices from CSV"))
            Import();
    }

    private void Import()
    {
        var feature = (AdsFeature)target;

        var so = new SerializedObject(feature);
        var csvProp = so.FindProperty("testDeviceCSV");

        var csvAsset = csvProp.objectReferenceValue as TextAsset;
        if (csvAsset == null)
        {
            Debug.LogWarning("[AdsFeatureEditor] testDeviceCSV is null.");
            return;
        }

        // name -> adId 로 파싱 (MAX 적용용)
        var parsed = TestDeviceCSVParser.ParseNameToAdId(csvAsset.text);

        if (!TrySetDictionaryViaReflection(feature, parsed))
        {
            Debug.LogError("[AdsFeatureEditor] Failed to set testDevices.");
            return;
        }

        EditorUtility.SetDirty(feature);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AdsFeatureEditor] Imported {parsed.Count} test devices from CSV.");
    }

    private bool TrySetDictionaryViaReflection(AdsFeature feature, System.Collections.Generic.Dictionary<string, string> parsed)
    {
        var f = typeof(AdsFeature).GetField("testDevices", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f == null) return false;

        if (f.GetValue(feature) is not Dictionary<string, string> dict)
        {
            dict = new Dictionary<string, string>();
            f.SetValue(feature, dict);
        }

        dict.Clear();
        foreach (var kv in parsed)
            dict[kv.Key] = kv.Value;

        return true;
    }
}
#endif
