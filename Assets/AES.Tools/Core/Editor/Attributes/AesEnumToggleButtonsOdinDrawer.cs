#if UNITY_EDITOR && ODIN_INSPECTOR
using AES.Tools.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEditor;

namespace AES.Tools.Gui.Editor
{
    public class AesEnumToggleButtonsOdinDrawer : OdinAttributeDrawer<AesEnumToggleButtonsAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (Property.ValueEntry == null)
            {
                CallNextDrawer(label);
                return;
            }

            var enumType = Property.ValueEntry.BaseValueType;
            if (!enumType.IsEnum)
            {
                EditorGUILayout.HelpBox("[AesEnumToggleButtons] 은 Enum 전용입니다.", MessageType.Error);
                CallNextDrawer(label);
                return;
            }

            // 라벨 표시
            if (label != null)
                EditorGUILayout.LabelField(label);

            EditorGUI.indentLevel++;

            string[] names = System.Enum.GetNames(enumType);
            var values = System.Enum.GetValues(enumType);

            int currentIndex = System.Array.IndexOf(values, Property.ValueEntry.WeakSmartValue);

            EditorGUI.BeginChangeCheck();
            int newIndex = GUILayout.Toolbar(currentIndex, names);
            if (EditorGUI.EndChangeCheck())
            {
                Property.ValueEntry.WeakSmartValue = values.GetValue(newIndex);
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif