#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AES.Tools.VContainer;

[CustomPropertyDrawer(typeof(PlacementConfig))]
public class PlacementConfigDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var networkProp = property.FindPropertyRelative("network");
        var adUnitIdProp = property.FindPropertyRelative("adUnitId");

        float line = EditorGUIUtility.singleLineHeight;
        float v    = EditorGUIUtility.standardVerticalSpacing;

        // 전체 박스
        var boxRect = new Rect(position.x, position.y, position.width,
            line * 2 + v * 3);
        GUI.Box(boxRect, GUIContent.none);

        // 헤더
        var headerRect = new Rect(position.x + 6, position.y + 3,
            position.width - 12, line);
        EditorGUI.LabelField(headerRect, label);

        // 네트워크 / 유닛ID 한 줄
        float y = headerRect.y + line + v;

        float totalInnerWidth = position.width - 16;
        float networkWidth    = totalInnerWidth * 0.35f;
        float idWidth         = totalInnerWidth - networkWidth - 4;

        var networkRect = new Rect(position.x + 8, y, networkWidth, line);
        var idRect      = new Rect(networkRect.xMax + 4, y, idWidth, line);

        EditorGUI.PropertyField(networkRect, networkProp, GUIContent.none);
        EditorGUI.PropertyField(idRect, adUnitIdProp, GUIContent.none);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float v    = EditorGUIUtility.standardVerticalSpacing;
        return line * 2 + v * 3;
    }
}
#endif
