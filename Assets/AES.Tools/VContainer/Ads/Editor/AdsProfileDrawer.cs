#if UNITY_EDITOR
using AES.Tools.VContainer;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AdsProfile))]
public class AdsProfileDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var environmentProp  = property.FindPropertyRelative("environment");
        var platformProp     = property.FindPropertyRelative("platform");
        var appOpenProp      = property.FindPropertyRelative("appOpen");
        var bannerProp       = property.FindPropertyRelative("banner");
        var interstitialProp = property.FindPropertyRelative("interstitial");
        var rewardedProp     = property.FindPropertyRelative("rewarded");

        float line = EditorGUIUtility.singleLineHeight;
        float v    = EditorGUIUtility.standardVerticalSpacing;

        // 헤더용 문자열 구성 (환경 / 플랫폼)
        string envName  = environmentProp.enumDisplayNames.Length > 0
            ? environmentProp.enumDisplayNames[Mathf.Clamp(environmentProp.enumValueIndex, 0, environmentProp.enumDisplayNames.Length - 1)]
            : "Environment";

        string platName = platformProp.enumDisplayNames.Length > 0
            ? platformProp.enumDisplayNames[Mathf.Clamp(platformProp.enumValueIndex, 0, platformProp.enumDisplayNames.Length - 1)]
            : "Platform";

        string dynamicLabel = $"{envName} / {platName}";

        // 전체 박스
        float totalHeight = GetPropertyHeight(property, label);
        var boxRect = new Rect(position.x, position.y, position.width, totalHeight);
        GUI.Box(boxRect, GUIContent.none);

        // Foldout 헤더
        var foldoutRect = new Rect(position.x + 6, position.y + 3,
            position.width - 12, line);
        property.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            property.isExpanded,
            dynamicLabel,
            true
        );

        if (!property.isExpanded)
            return;

        EditorGUI.indentLevel++;
        float y = foldoutRect.y + line + v * 2;

        // ============================
        // 환경 / 플랫폼 각각 한 줄 전체폭
        // ============================
        float innerX     = position.x + 10;
        float innerWidth = position.width - 20;

        var envRect = new Rect(innerX, y, innerWidth, line);
        EditorGUI.PropertyField(envRect, environmentProp, new GUIContent("환경"));
        y += line + v;

        var platRect = new Rect(innerX, y, innerWidth, line);
        EditorGUI.PropertyField(platRect, platformProp, new GUIContent("플랫폼"));
        y += line + v * 2;
        // ============================

        // PlacementConfig 들
        y = DrawPlacementBlock(innerX, y, innerWidth, appOpenProp,     "앱 오픈");
        y = DrawPlacementBlock(innerX, y, innerWidth, bannerProp,      "배너 광고");
        y = DrawPlacementBlock(innerX, y, innerWidth, interstitialProp,"전면 광고");
        y = DrawPlacementBlock(innerX, y, innerWidth, rewardedProp,    "보상 광고");

        EditorGUI.indentLevel--;
    }

    private float DrawPlacementBlock(float x, float y, float width,
        SerializedProperty prop, string label)
    {
        float v    = EditorGUIUtility.standardVerticalSpacing;
        float h    = EditorGUI.GetPropertyHeight(prop, true);

        var rect = new Rect(x, y, width, h);
        EditorGUI.PropertyField(rect, prop, new GUIContent(label), true);

        return y + h + v;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float v    = EditorGUIUtility.standardVerticalSpacing;

        if (!property.isExpanded)
            return line + v * 2;

        var environmentProp  = property.FindPropertyRelative("environment");
        var platformProp     = property.FindPropertyRelative("platform");
        var appOpenProp      = property.FindPropertyRelative("appOpen");
        var bannerProp       = property.FindPropertyRelative("banner");
        var interstitialProp = property.FindPropertyRelative("interstitial");
        var rewardedProp     = property.FindPropertyRelative("rewarded");

        float h = 0f;

        // 박스 안 여백 + 헤더 줄
        h += line + v * 3;

        // 환경 + 플랫폼 두 줄
        h += line * 2 + v * 3;

        // PlacementConfig 네 개
        h += EditorGUI.GetPropertyHeight(appOpenProp, true)      + v;
        h += EditorGUI.GetPropertyHeight(bannerProp, true)       + v;
        h += EditorGUI.GetPropertyHeight(interstitialProp, true) + v;
        h += EditorGUI.GetPropertyHeight(rewardedProp, true)     + v;

        // 박스 하단 여유
        h += v;

        return h;
    }
}
#endif
