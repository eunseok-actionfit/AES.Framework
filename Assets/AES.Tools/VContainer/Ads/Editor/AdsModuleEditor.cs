#if UNITY_EDITOR
using AES.Tools.VContainer.Bootstrap;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AdsModule))]
public class AdsModuleEditor : Editor
{
    SerializedProperty scriptProp;

    SerializedProperty enableAdsProp;
    SerializedProperty profilesProp;
    SerializedProperty currentEnvironmentProp;

    SerializedProperty testDeviceMapProp;
    SerializedProperty testDeviceCsvProp;

    private void OnEnable()
    {
        scriptProp = serializedObject.FindProperty("m_Script");

        enableAdsProp          = serializedObject.FindProperty("enableAds");
        profilesProp           = serializedObject.FindProperty("profiles");
        currentEnvironmentProp = serializedObject.FindProperty("currentEnvironment");

        // ★ MAX Test Device 연동 부분
        testDeviceMapProp = serializedObject.FindProperty("_testDeviceMap");
        testDeviceCsvProp = serializedObject.FindProperty("_testDeviceCsv");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script (Read Only)
        EditorGUI.BeginDisabledGroup(true);
        if (scriptProp != null)
            EditorGUILayout.PropertyField(scriptProp);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        // 광고 전체 ON/OFF
        EditorGUILayout.PropertyField(enableAdsProp, new GUIContent("광고 전체 ON/OFF"));

        EditorGUILayout.Space();

        // 프로필 목록
        EditorGUILayout.LabelField("프로필 목록 (환경/플랫폼별)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(profilesProp, GUIContent.none, true);

        EditorGUILayout.Space();

        // 현재 환경
        EditorGUILayout.PropertyField(currentEnvironmentProp, new GUIContent("현재 환경"));

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // ---------------------------
        // ★ MAX Test Device Section
        // ---------------------------
        EditorGUILayout.LabelField("MAX Test Devices", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(testDeviceCsvProp, new GUIContent("Test Device CSV"));
        EditorGUILayout.PropertyField(testDeviceMapProp, new GUIContent("Test Device Map"), true);

        EditorGUILayout.Space();

        if (GUILayout.Button("⬆ CSV → Test Device Map 매핑하기"))
        {
            ApplyCsvToTestDeviceMap();
        }

        EditorGUILayout.Space();

        serializedObject.ApplyModifiedProperties();
    }

    private void ApplyCsvToTestDeviceMap()
    {
        var adsModule = (AdsModule)target;

        if (adsModule == null)
            return;

        var csv = adsModule.GetType()
            .GetField("_testDeviceCsv", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(adsModule) as TextAsset;

        var map = adsModule.GetType()
            .GetField("_testDeviceMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(adsModule) as System.Collections.IDictionary;

        if (csv == null)
        {
            Debug.LogWarning("CSV 파일이 없습니다.");
            return;
        }

        if (map == null)
        {
            Debug.LogError("_testDeviceMap 이 null 입니다. SerializedDictionary가 정상적으로 초기화되었는지 확인하세요.");
            return;
        }

        // 기존 map 초기화 여부는 선택 — 여기서는 덮어쓰기 방식 유지
        map.Clear();

        var lines = csv.text.Split('\n');
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line))
                continue;
            if (line.StartsWith("#"))
                continue;

            var cols = line.Split(',');
            if (cols.Length < 2)
                continue;

            var name = cols[0].Trim();
            var id   = cols[1].Trim();

            if (string.IsNullOrEmpty(id))
                continue;

            map[name] = id;
        }

        Debug.Log($"CSV 매핑 완료 — 총 {map.Count}개의 테스트 디바이스가 등록되었습니다.");

        // 에디터 저장 플래그
        EditorUtility.SetDirty(adsModule);
        AssetDatabase.SaveAssets();
    }
}
#endif
