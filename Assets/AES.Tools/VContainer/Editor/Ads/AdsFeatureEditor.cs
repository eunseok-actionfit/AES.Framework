#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AES.Tools.VContainer.Bootstrap.Framework.Features;

[CustomEditor(typeof(AdsFeature))]
public sealed class AdsFeatureEditor : Editor
{
    SerializedProperty scriptProp;

    // AppFeatureSO base fields (private [SerializeField])
    SerializedProperty idProp;
    SerializedProperty orderProp;
    SerializedProperty dependsOnProp;
    SerializedProperty enabledByDefaultProp;

    // AdsFeature fields
    SerializedProperty enableAdsProp;
    SerializedProperty profilesProp;
    SerializedProperty currentEnvironmentProp;

    SerializedProperty interstitialMinIntervalSecondsProp;
    SerializedProperty interstitialMaxPerSessionProp;

    SerializedProperty runtimeAdsDisabledProp;
    SerializedProperty testDeviceCsvProp; // AdsFeature: testDeviceCSV

    void OnEnable()
    {
        scriptProp = serializedObject.FindProperty("m_Script");

        idProp = serializedObject.FindProperty("id");
        orderProp = serializedObject.FindProperty("order");
        dependsOnProp = serializedObject.FindProperty("dependsOn");
        enabledByDefaultProp = serializedObject.FindProperty("enabledByDefault");

        enableAdsProp = serializedObject.FindProperty("enableAds");
        profilesProp = serializedObject.FindProperty("profiles");
        currentEnvironmentProp = serializedObject.FindProperty("currentEnvironment");

        interstitialMinIntervalSecondsProp = serializedObject.FindProperty("interstitialMinIntervalSeconds");
        interstitialMaxPerSessionProp = serializedObject.FindProperty("interstitialMaxPerSession");

        runtimeAdsDisabledProp = serializedObject.FindProperty("runtimeAdsDisabled");
        testDeviceCsvProp = serializedObject.FindProperty("testDeviceCSV");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script (read-only)
        using (new EditorGUI.DisabledScope(true))
        {
            if (scriptProp != null)
                EditorGUILayout.PropertyField(scriptProp);
        }

        // Base (AppFeatureSO)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Feature Meta", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(idProp, new GUIContent("Id"));
        EditorGUILayout.PropertyField(orderProp, new GUIContent("Order"));
        EditorGUILayout.PropertyField(dependsOnProp, new GUIContent("Depends On"), true);
        EditorGUILayout.PropertyField(enabledByDefaultProp, new GUIContent("Enabled By Default"));

        // Ads
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ads", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableAdsProp, new GUIContent("Ads 전체 ON/OFF"));
        EditorGUILayout.PropertyField(currentEnvironmentProp, new GUIContent("현재 환경"));
        EditorGUILayout.PropertyField(profilesProp, new GUIContent("Profiles"), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Interstitial Rules", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(interstitialMinIntervalSecondsProp, new GUIContent("Min Interval Seconds"));
        EditorGUILayout.PropertyField(interstitialMaxPerSessionProp, new GUIContent("Max Per Session"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Flags", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(runtimeAdsDisabledProp, new GUIContent("Runtime Ads Disabled"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Test Device CSV", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(testDeviceCsvProp, new GUIContent("Test Device CSV"));

        // (선택) CSV 유효성 체크 버튼 정도만
        if (GUILayout.Button("Validate CSV"))
        {
            var csv = testDeviceCsvProp.objectReferenceValue as TextAsset;
            if (!csv)
            {
                Debug.LogWarning("[AdsFeature] CSV is null.");
            }
            else
            {
                Debug.Log($"[AdsFeature] CSV bytes={csv.bytes?.Length ?? 0}");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
