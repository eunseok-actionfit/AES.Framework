#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UITextStyle용 커스텀 인스펙터
/// - fontAsset(TMP_FontAsset)를 선택하면
///   해당 폰트 아틀라스를 사용하는 TMP 머티리얼들을 드롭다운(Material Preset)으로 노출
/// - 멀티 에디팅 지원
/// </summary>
[CustomEditor(typeof(UITextStyle))]
[CanEditMultipleObjects]
public class UITextStyleEditor : Editor
{
    SerializedProperty fontAssetProp;
    SerializedProperty fontMaterialProp;
    SerializedProperty fontSizeProp;
    SerializedProperty lineSpacingProp;
    SerializedProperty characterSpacingProp;
    SerializedProperty colorProp;
    SerializedProperty richTextProp;
    SerializedProperty raycastTargetProp;

    void OnEnable()
    {
        fontAssetProp        = serializedObject.FindProperty("fontAsset");
        fontMaterialProp     = serializedObject.FindProperty("fontMaterial");
        fontSizeProp         = serializedObject.FindProperty("fontSize");
        lineSpacingProp      = serializedObject.FindProperty("lineSpacing");
        characterSpacingProp = serializedObject.FindProperty("characterSpacing");
        colorProp            = serializedObject.FindProperty("color");
        richTextProp         = serializedObject.FindProperty("richText");
        raycastTargetProp    = serializedObject.FindProperty("raycastTarget");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1) Font Asset
        EditorGUILayout.PropertyField(fontAssetProp);

        // 2) Font Material (TMP Material Preset 드롭다운)
        DrawFontMaterialPresetField();

        EditorGUILayout.Space();

        // 3) 나머지 필드
        EditorGUILayout.PropertyField(fontSizeProp);
        EditorGUILayout.PropertyField(lineSpacingProp);
        EditorGUILayout.PropertyField(characterSpacingProp);
        EditorGUILayout.PropertyField(colorProp);
        EditorGUILayout.PropertyField(richTextProp);
        EditorGUILayout.PropertyField(raycastTargetProp);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawFontMaterialPresetField()
    {
        // 여러 UITextStyle이 서로 다른 FontAsset을 가질 때
        if (fontAssetProp.hasMultipleDifferentValues)
        {
            EditorGUILayout.HelpBox(
                "선택된 UITextStyle 들의 Font Asset 이 서로 다릅니다.\n" +
                "같은 Font Asset 으로 맞추면 Material Preset 을 한 번에 설정할 수 있습니다.",
                MessageType.Info
            );
            // 이 경우에는 그냥 일반 필드로 노출
            EditorGUILayout.PropertyField(fontMaterialProp);
            return;
        }

        var fontAsset = fontAssetProp.objectReferenceValue as TMP_FontAsset;

        if (fontAsset == null)
        {
            EditorGUILayout.HelpBox("Font Asset 을 먼저 선택해주세요.", MessageType.Info);
            EditorGUILayout.PropertyField(fontMaterialProp);
            return;
        }

        // 해당 폰트 아틀라스를 사용하는 TMP 머티리얼들 수집
        List<Material> presets = FindTMPMaterialPresetsWithDefault(fontAsset);

        if (presets == null || presets.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "이 Font Asset 을 사용하는 TMP Material Preset 을 찾을 수 없습니다.\n" +
                "같은 atlasTexture 를 사용하는 TMP 머티리얼을 만들어 두어야 합니다.",
                MessageType.Warning
            );
            EditorGUILayout.PropertyField(fontMaterialProp);
            return;
        }

        // 현재 선택된 머티리얼
        Object currentMat = fontMaterialProp.objectReferenceValue;

        int currentIndex = -1;
        string[] names = new string[presets.Count];

        for (int i = 0; i < presets.Count; i++)
        {
            // 0번은 기본 머티리얼이라는 표시
            names[i] = (i == 0) ? "[Default] " + presets[i].name : presets[i].name;

            if (currentMat == presets[i])
                currentIndex = i;
        }

        if (currentIndex < 0)
            currentIndex = 0;

        // 멀티 에디트에서 서로 다른 머티리얼 값을 가지면 mixed 표시
        bool mixed = fontMaterialProp.hasMultipleDifferentValues;
        EditorGUI.showMixedValue = mixed;

        int newIndex = EditorGUILayout.Popup(new GUIContent("Font Material Preset"), currentIndex, names);

        EditorGUI.showMixedValue = false;

        if (newIndex >= 0 && newIndex < presets.Count)
        {
            fontMaterialProp.objectReferenceValue = presets[newIndex];
        }
    }

    /// <summary>
    /// 지정한 TMP_FontAsset 과 같은 atlasTexture 를 사용하는
    /// TextMeshPro용 머티리얼들을 모두 찾는다.
    /// 0번 요소는 항상 fontAsset.material 이 되도록 구성.
    /// </summary>
    List<Material> FindTMPMaterialPresetsWithDefault(TMP_FontAsset fontAsset)
    {
        var results = new List<Material>();

        // ① 기본 머티리얼을 항상 첫 번째로 추가
        if (fontAsset.material != null)
            results.Add(fontAsset.material);

        // ② 프로젝트 전체에서 TMP 머티리얼 검색
        string[] guids = AssetDatabase.FindAssets("t:Material");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
                continue;

            // TextMeshPro 계열 Shader만
            if (mat.shader == null || !mat.shader.name.Contains("TextMeshPro"))
                continue;

            // 같은 atlasTexture 를 사용하는 경우만
            if (mat.GetTexture("_MainTex") == fontAsset.atlasTexture)
            {
                if (!ReferenceEquals(mat, fontAsset.material))
                    results.Add(mat);
            }
        }

        return results;
    }
}
#endif
