#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AdsProfile))]
public class AdsProfileDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var environmentProp = property.FindPropertyRelative("environment");
        var platformProp    = property.FindPropertyRelative("platform");

        string dynamicLabel = $"{environmentProp.enumDisplayNames[environmentProp.enumValueIndex]} / " +
                              $"{platformProp.enumDisplayNames[platformProp.enumValueIndex]}";

        // foldout
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            dynamicLabel,
            true
        );

        if (!property.isExpanded)
            return;

        EditorGUI.indentLevel++;

        var y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
            environmentProp
        );

        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.PropertyField(
            new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight),
            platformProp
        );

        y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // 나머지 필드 자동 처리
        SerializedProperty iterator = property.Copy();
        SerializedProperty end = iterator.GetEndProperty();

        iterator.NextVisible(true); // 첫 child
        iterator.NextVisible(false); // environment 건너뜀
        iterator.NextVisible(false); // platform 건너뜀

        while (!SerializedProperty.EqualContents(iterator, end))
        {
            float h = EditorGUI.GetPropertyHeight(iterator, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), iterator, true);
            y += h + EditorGUIUtility.standardVerticalSpacing;
            iterator.NextVisible(false);
        }

        EditorGUI.indentLevel--;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float h = EditorGUIUtility.singleLineHeight * 2 +
                  EditorGUIUtility.standardVerticalSpacing * 2;

        SerializedProperty iterator = property.Copy();
        SerializedProperty end = iterator.GetEndProperty();

        iterator.NextVisible(true);
        iterator.NextVisible(false);
        iterator.NextVisible(false);

        while (!SerializedProperty.EqualContents(iterator, end))
        {
            h += EditorGUI.GetPropertyHeight(iterator, true) +
                 EditorGUIUtility.standardVerticalSpacing;
            iterator.NextVisible(false);
        }

        return h;
    }
}
#endif
