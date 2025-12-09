#if UNITY_EDITOR && ODIN_INSPECTOR
using AES.Tools.Gui;
using AES.Tools.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Gui.Editor
{
    public class AesInformationOdinDrawer : OdinAttributeDrawer<AesInformationAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var attr = Attribute;

            // Help 꺼짐 또는 메시지 없음 → 필드만 표시
            if (!MenuHelp.HelpEnabled || string.IsNullOrEmpty(attr.Message))
            {
                CallNextDrawer(label);
                return;
            }

            if (!attr.MessageAfterProperty)
            {
                EditorGUILayout.HelpBox(attr.Message, attr.Type);
            }

            CallNextDrawer(label);

            if (attr.MessageAfterProperty)
            {
                EditorGUILayout.HelpBox(attr.Message, attr.Type);
            }
        }
    }
}
#endif